using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Outbox.SqlServer
{
    public class SqlServerOutboxStorage : IOutboxStorage
    {
        private readonly DbConnectionAccessor connectionAccessor;
        private readonly string address;
        private readonly TableName tableName;
        private static readonly HeaderSerializer HeaderSerializer = new();
        private readonly int addressLength;

        public SqlServerOutboxStorage(SqlServerOutboxSettings sqlServerOutboxSettings,
            DbConnectionAccessor connectionAccessor, string address)
        {
            this.connectionAccessor = connectionAccessor;
            this.address = address;
            this.addressLength = address.Length;
            this.tableName = TableName.Parse(sqlServerOutboxSettings.TableName);
        }

        public async Task Store(TransportMessage message, IEnumerable<TransportMessage> outgoingMessages)
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.Transaction = connectionAccessor.Item.DbTransaction;

                var messageId = message.GetMessageId();

                command.Parameters.Add("messageId", SqlDbType.NChar, messageId.Length).Value = messageId;
                command.Parameters.Add("messageInputQueue", SqlDbType.NChar, addressLength).Value = address;
                
                command.CommandText = string.Empty;
                var i = 0;
                
                foreach (var outMessage in outgoingMessages)
                {
                    command.CommandText += $@"INSERT INTO {tableName} ([MessageId], [MessageInputQueue], [Headers], [Body]) VALUES (@messageId, @messageInputQueue, @headers{i}, @body{i});";

                    var serializedHeaders = HeaderSerializer.Serialize(outMessage.Headers);

                    command.Parameters.Add($"headers{i}", SqlDbType.VarBinary, MathUtil.GetNextPowerOfTwo(serializedHeaders.Length)).Value = serializedHeaders;
                    command.Parameters.Add($"body{i}", SqlDbType.VarBinary, MathUtil.GetNextPowerOfTwo(outMessage.Body.Length)).Value = outMessage.Body;
                    
                    ++i;
                }

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteOutgoingMessages(TransportMessage message)
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.Transaction = connectionAccessor.Item.DbTransaction;
                command.CommandText = $"DELETE FROM {tableName} WHERE MessageID = @messageId AND MessageInputQueue = @messageInputQueue AND LEN(Body) <> 0";

                var messageId = message.GetMessageId();

                command.Parameters.Add("messageId", SqlDbType.NChar, messageId.Length).Value = messageId;
                command.Parameters.Add("messageInputQueue", SqlDbType.NChar, addressLength).Value = address;

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteIdempotencyCheckMessage(TransportMessage message)
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.Transaction = connectionAccessor.Item.DbTransaction;
                command.CommandText = $"DELETE FROM {tableName} WHERE MessageID = @messageId AND MessageInputQueue = @messageInputQueue AND LEN(Body) = 0";

                var messageId = message.GetMessageId();

                command.Parameters.Add("messageId", SqlDbType.NChar, messageId.Length).Value = messageId;
                command.Parameters.Add("messageInputQueue", SqlDbType.NChar, addressLength).Value = address;

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <inheritdoc cref="IOutboxStorage.GetOutgoingMessages"/>
        public async Task<List<TransportMessage>> GetOutgoingMessages(TransportMessage message)
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.Transaction = connectionAccessor.Item.DbTransaction;
                command.CommandText = @$"SELECT [Headers], [Body] FROM {tableName} WHERE [MessageId] = @messageId AND [MessageInputQueue] = @messageInputQueue";

                var messageId = message.GetMessageId();

                command.Parameters.Add("messageId", SqlDbType.NChar, messageId.Length).Value = messageId;
                command.Parameters.Add("messageInputQueue", SqlDbType.NChar, addressLength).Value = address;

                var transportMessages = new List<TransportMessage>();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var headers = reader["headers"];
                        var headersDictionary = HeaderSerializer.Deserialize((byte[])headers);
                        var body = (byte[])reader["body"];

                        transportMessages.Add(new TransportMessage(headersDictionary, body));
                    }
                }

                return transportMessages;
            }
        }

        public async Task EnsureTableIsCreated()
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.CommandText = $@"
                        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{tableName.Schema}')
	                        EXEC('CREATE SCHEMA {tableName.Schema}')
                        ----
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{tableName.Schema}' AND TABLE_NAME = '{
                                                tableName.Name
                                            }') 
                            CREATE TABLE {tableName}(
	                            [Id] [bigint] IDENTITY(1,1) NOT NULL,
	                            [MessageId] [nvarchar](255) NOT NULL,
	                            [MessageInputQueue] [nvarchar](255) NOT NULL,
	                            [Headers] [varbinary](max) NOT NULL,
	                            [Body] [varbinary](max) NOT NULL,
	                            [Handled] [datetime] NOT NULL,
                             CONSTRAINT [PK_{tableName.Schema}_{tableName.Name}] PRIMARY KEY CLUSTERED 
                            (
	                            [Id] ASC
                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
                       -----
                       IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE [name] = (N'DF_{tableName.Schema}_{tableName.Name}_Created') AND type = 'D')
                            ALTER TABLE {tableName} ADD  CONSTRAINT [DF_{tableName.Schema}_{tableName.Name}_Created]  DEFAULT (getutcdate()) FOR [Handled]
                       -----
                       IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_{tableName.Schema}_{tableName.Name}_MessageId_MessageInputQueue')
                            CREATE NONCLUSTERED INDEX [IX_{tableName.Schema}_{tableName.Name}_MessageId_MessageInputQueue] ON {tableName}
                            (
	                            [MessageId] ASC,
	                            [MessageInputQueue] ASC
                            )
                            INCLUDE([Headers],[Body]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                       -----
                       IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_{tableName.Schema}_{tableName.Name}_Handled')
                            CREATE NONCLUSTERED INDEX [IX_{tableName.Schema}_{tableName.Name}_Handled] ON {tableName}
                            (
	                            [Handled] ASC
                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                        ";
                command.Transaction = connectionAccessor.Item.DbTransaction;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}