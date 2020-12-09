using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Rebus.Messages;

namespace Rebus.Outbox.SqlServer
{
    public class SqlServerOutboxStorage : IOutboxStorage
    {
        private readonly DbConnectionAccessor connectionAccessor;
        private readonly TableName tableName;

        public SqlServerOutboxStorage(SqlServerOutboxSettings sqlServerOutboxSettings, DbConnectionAccessor connectionAccessor)
        {
            this.connectionAccessor = connectionAccessor;
            this.tableName = TableName.Parse(sqlServerOutboxSettings.TableName);
        }

        /// <inheritdoc cref="IOutboxStorage.GetOutgoingMessages"/>
        public async Task<List<TransportMessage>> GetOutgoingMessages(Message message)
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.Transaction = connectionAccessor.Item.DbTransaction;
                command.CommandText = $"SELECT Data FROM {tableName} WHERE Id = @id";
                var idParameter = command.CreateParameter();
                idParameter.ParameterName = "Id";
                idParameter.DbType = DbType.Guid;
                idParameter.Size = -1;
                idParameter.Value = Guid.Parse(message.Headers[Headers.MessageId]);
                command.Parameters.Add(idParameter);

                var result = await command.ExecuteScalarAsync() as byte[];
                if (result == null)
                    return null;

                return System.Text.Json.JsonSerializer.Deserialize<List<TransportMessage>>(result);
            }
        }

        /// <inheritdoc cref="IOutboxStorage.TryStore"/>
        public async Task<bool> TryStore(Message message, List<TransportMessage> outgoingMessages)
        {
            using (var command = connectionAccessor.Item.DbConnection.CreateCommand())
            {
                command.Transaction = connectionAccessor.Item.DbTransaction;
                command.CommandText =
                    $@"INSERT INTO {tableName} ([Id], [Data]) VALUES (@Id, @Data)";

                var idParam = command.CreateParameter();
                idParam.ParameterName = "Id";
                idParam.DbType = DbType.Guid;
                idParam.Size = -1;
                idParam.Value = Guid.Parse(message.Headers[Headers.MessageId]);
                command.Parameters.Add(idParam);

                var data = command.CreateParameter();
                data.ParameterName = "data";
                data.DbType = DbType.Binary;
                data.Size = -1;
                data.Value = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(outgoingMessages);
                command.Parameters.Add(data);

                try
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
                catch (SqlException e) when (e.Number == 2627)
                {
                    return false;
                }
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
                            CREATE TABLE {tableName} (
                                [id] [uniqueidentifier] NOT NULL,
	                            [data] [varbinary](MAX) NOT NULL,
                                CONSTRAINT [PK_{tableName.Schema}_{tableName.Name}] PRIMARY KEY NONCLUSTERED 
                                (
	                                [id] ASC
                                )
                            )
                        ";
                command.Transaction = connectionAccessor.Item.DbTransaction;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}