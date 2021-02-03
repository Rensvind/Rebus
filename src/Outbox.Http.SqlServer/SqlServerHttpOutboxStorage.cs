using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Outbox.SqlServer.Common;
using Rebus.Serialization;

namespace Rebus.Outbox.Http.SqlServer
{
    public class SqlServerHttpOutboxStorage : IOutboxHttpStorage
    {
        private readonly string applicationId;
        private readonly int applicationIdLength;
        private static readonly HeaderSerializer HeaderSerializer = new();
        private readonly TableName tableName;

        public SqlServerHttpOutboxStorage(string tableName, string applicationId)
        {
            this.applicationId = applicationId;
            this.applicationIdLength = applicationId.Length;
            this.tableName = TableName.Parse(tableName);
        }

        public async Task Store(string operationId, IEnumerable<TransportMessage> outgoingMessages)
        {
            var sqlServerOutboxTransaction = OutboxTransaction.Get();

            IEnumerable<TransportMessage> batch;

            const int batchSize = 2000;
            var batchCnt = 0;

            while ((batch = outgoingMessages.Skip(batchSize * batchCnt++).Take(batchSize)).Any())
            {
                using (var command = sqlServerOutboxTransaction.Connection.CreateCommand())
                {
                    command.Transaction = sqlServerOutboxTransaction.Transaction;

                    command.Parameters.Add("operationId", SqlDbType.NChar, operationId.Length).Value = operationId;
                    command.Parameters.Add("applicationId", SqlDbType.NChar, applicationIdLength).Value = applicationId;

                    command.CommandText = string.Empty;
                    var i = 0;

                    foreach (var outMessage in batch)
                    {
                        command.CommandText +=
                            $@"INSERT INTO {tableName} ([OperationId], [ApplicationId], [Headers], [Body]) VALUES (@operationId, @applicationId, @headers{i}, @body{i});";

                        var serializedHeaders = HeaderSerializer.Serialize(outMessage.Headers);

                        command.Parameters.Add($"headers{i}", SqlDbType.VarBinary,
                            MathUtil.GetNextPowerOfTwo(serializedHeaders.Length)).Value = serializedHeaders;
                        command.Parameters.Add($"body{i}", SqlDbType.VarBinary,
                            MathUtil.GetNextPowerOfTwo(outMessage.Body.Length)).Value = outMessage.Body;

                        ++i;
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteOutgoingMessages(string operationId)
        {
            var sqlServerOutboxTransaction = OutboxTransaction.Get();

            using (var command = sqlServerOutboxTransaction.Connection.CreateCommand())
            {
                command.Transaction = sqlServerOutboxTransaction.Transaction;
                command.CommandText = $"DELETE FROM {tableName} WHERE OperationId = @operationId AND ApplicationId = @applicationId";

                command.Parameters.Add("operationId", SqlDbType.NChar, operationId.Length).Value = operationId;
                command.Parameters.Add("applicationId", SqlDbType.NChar, applicationIdLength).Value = applicationId;

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<TransportMessage>> GetUnsentOutgoingMessages(int topMessages, TimeSpan messagesOlderThan)
        {
            var sqlServerOutboxTransaction = OutboxTransaction.Get();

            using (var command = sqlServerOutboxTransaction.Connection.CreateCommand())
            {
                command.Transaction = sqlServerOutboxTransaction.Transaction;
                command.CommandText = @$"DELETE TOP({ topMessages}) 
                FROM {tableName}
                WITH(READPAST, ROWLOCK, READCOMMITTEDLOCK)
                OUTPUT deleted.Headers, deleted.Body
                WHERE [ApplicationId] = @ApplicationId AND [Timestamp] < @timestamp";

                command.Parameters.Add("applicationId", SqlDbType.NChar, applicationIdLength).Value = applicationId;
                command.Parameters.Add("timestamp", SqlDbType.DateTime).Value = DateTime.UtcNow.Add(messagesOlderThan); // NOTE: We must subtract so we don't send messages that has just been added by a http request

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
            var sqlServerOutboxTransaction = OutboxTransaction.Get();

            using (var command = sqlServerOutboxTransaction.Connection.CreateCommand())
            {
                command.Transaction = sqlServerOutboxTransaction.Transaction;

                command.CommandText = $@"
                        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{tableName.Schema}')
	                        EXEC('CREATE SCHEMA {tableName.Schema}')
                        ----
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{tableName.Schema}' AND TABLE_NAME = '{
                                                tableName.Name
                                            }') 
                            CREATE TABLE {tableName}(
	                            [Id] [bigint] IDENTITY(1,1) NOT NULL,
	                            [OperationId] [nvarchar](255) NOT NULL,
	                            [ApplicationId] [nvarchar](255) NOT NULL,
	                            [Headers] [varbinary](max) NOT NULL,
	                            [Body] [varbinary](max) NOT NULL,
	                            [Timestamp] [datetime] NOT NULL,
                             CONSTRAINT [PK_{tableName.Schema}_{tableName.Name}] PRIMARY KEY CLUSTERED 
                            (
	                            [Id] ASC
                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
                       -----
                       IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE [name] = (N'DF_{tableName.Schema}_{tableName.Name}_Timestamp') AND type = 'D')
                            ALTER TABLE {tableName} ADD  CONSTRAINT [DF_{tableName.Schema}_{tableName.Name}_Timestamp]  DEFAULT (getutcdate()) FOR [Timestamp]
                       -----
                       IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_{tableName.Schema}_{tableName.Name}_OperationId_ApplicationId')
                            CREATE NONCLUSTERED INDEX [IX_{tableName.Schema}_{tableName.Name}_OperationId_ApplicationId] ON {tableName}
                            (
	                            [OperationId] ASC,
	                            [ApplicationId] ASC
                            )
                            INCLUDE([Headers],[Body]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                       -----
                       IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_{tableName.Schema}_{tableName.Name}_ApplicationId_Timestamp')
                            CREATE NONCLUSTERED INDEX [IX_{tableName.Schema}_{tableName.Name}_ApplicationId_Timestamp] ON {tableName}
                            (
                                [ApplicationId] ASC,
	                            [Timestamp] ASC
                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                        ";

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
