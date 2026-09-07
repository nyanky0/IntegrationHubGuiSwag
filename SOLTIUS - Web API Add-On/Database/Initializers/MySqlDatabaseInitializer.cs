using Dapper;
using MySql.Data.MySqlClient;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;

namespace SOLTIUS_Web_API_Add_On.Database.Initializers
{
    public class MySqlDatabaseInitializer : IDatabaseInitializer
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        public MySqlDatabaseInitializer(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InitializeAsync(DBConfig config)
        {
            using var connection = (MySqlConnection)_connectionFactory.CreateConnection(config);
            await connection.OpenAsync();

            await CreateSalesOrderHeader(connection);
            await CreateSalesOrderDetail(connection);
            await CreatePurchaseOrderHeader(connection);
            await CreatePurchaseOrderDetail(connection);
        }

        private async Task CreateSalesOrderHeader(MySqlConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS sales_order_header
            (
                id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                cardcode VARCHAR(30) NOT NULL,
                cardname VARCHAR(200) NOT NULL,
                docdate DATETIME NOT NULL,
                docduedate DATETIME NOT NULL,
                taxdate DATETIME NOT NULL,
                remarks VARCHAR(254),
                process_status TINYINT NOT NULL DEFAULT 0,
                retrycount INT NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME,
                processed_at DATETIME,
                errormessage TEXT
            );";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateSalesOrderDetail(MySqlConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS sales_order_detail
            (
                id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                header_id BIGINT NOT NULL,
                linenum INT NOT NULL,
                itemcode VARCHAR(30) NOT NULL,
                itemname VARCHAR(200) NOT NULL,
                warehouse VARCHAR(20),
                quantity DECIMAL(19,6) NOT NULL,
                price DECIMAL(19,6),
                process_status TINYINT NOT NULL DEFAULT 0,
                retrycount INT NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME,
                processed_at DATETIME,
                errormessage TEXT
            );";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreatePurchaseOrderHeader(MySqlConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS SOL_PURCHASE_ORDER_HEADER
            (
                SOL_ID BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                SOL_CARDCODE VARCHAR(30) NOT NULL,
                SOL_CARDNAME VARCHAR(200) NOT NULL,
                SOL_DOCDATE DATETIME NOT NULL,
                SOL_DOCDUEDATE DATETIME NOT NULL,
                SOL_TAXDATE DATETIME NOT NULL,
                SOL_REMARKS VARCHAR(254),
                SOL_WEB_TX_NUMBER VARCHAR(50) NULL,
                SOL_WEB_TX_ID BIGINT NULL,
                SOL_UDF_DATA LONGTEXT NULL,
                SOL_PROCESS_STATUS TINYINT NOT NULL DEFAULT 0,
                SOL_RETRYCOUNT INT NOT NULL DEFAULT 0,
                SOL_DOCENTRY VARCHAR(50),
                SOL_CREATED_AT DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                SOL_UPDATED_AT DATETIME,
                SOL_PROCESSED_AT DATETIME,
                SOL_ERRORMESSAGE TEXT
            );";

            await connection.ExecuteAsync(sql);
            await EnsureColumnExistsAsync(connection, "SOL_PURCHASE_ORDER_HEADER", "SOL_WEB_TX_NUMBER", "VARCHAR(50) NULL");
            await EnsureColumnExistsAsync(connection, "SOL_PURCHASE_ORDER_HEADER", "SOL_WEB_TX_ID", "BIGINT NULL");
            await EnsureColumnExistsAsync(connection, "SOL_PURCHASE_ORDER_HEADER", "SOL_UDF_DATA", "LONGTEXT NULL");
        }

        private async Task CreatePurchaseOrderDetail(MySqlConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS SOL_PURCHASE_ORDER_DETAIL
            (
                SOL_ID BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                SOL_HEADER_ID BIGINT NOT NULL,
                SOL_LINENUM INT NOT NULL,
                SOL_ITEMCODE VARCHAR(30) NOT NULL,
                SOL_ITEMNAME VARCHAR(200) NOT NULL,
                SOL_WAREHOUSE VARCHAR(20),
                SOL_QUANTITY DECIMAL(19,6) NOT NULL,
                SOL_PRICE DECIMAL(19,6),
                SOL_VAT_GROUP VARCHAR(20) NULL,
                SOL_WEB_LINE_ID BIGINT NULL,
                SOL_UDF_DATA LONGTEXT NULL,
                SOL_PROCESS_STATUS TINYINT NOT NULL DEFAULT 0,
                SOL_RETRYCOUNT INT NOT NULL DEFAULT 0,
                SOL_CREATED_AT DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                SOL_UPDATED_AT DATETIME,
                SOL_PROCESSED_AT DATETIME,
                SOL_ERRORMESSAGE TEXT
            );";

            await connection.ExecuteAsync(sql);
            await EnsureColumnExistsAsync(connection, "SOL_PURCHASE_ORDER_DETAIL", "SOL_VAT_GROUP", "VARCHAR(20) NULL");
            await EnsureColumnExistsAsync(connection, "SOL_PURCHASE_ORDER_DETAIL", "SOL_WEB_LINE_ID", "BIGINT NULL");
            await EnsureColumnExistsAsync(connection, "SOL_PURCHASE_ORDER_DETAIL", "SOL_UDF_DATA", "LONGTEXT NULL");
        }

        private static async Task EnsureColumnExistsAsync(MySqlConnection connection, string tableName, string columnName, string columnDefinition)
        {
            string checkSql = @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName;";
            int count = await connection.ExecuteScalarAsync<int>(checkSql, new { TableName = tableName, ColumnName = columnName });
            if (count == 0)
            {
                await connection.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
            }
        }
    }
}
