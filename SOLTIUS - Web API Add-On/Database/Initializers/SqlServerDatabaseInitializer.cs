using Dapper;
using Microsoft.Data.SqlClient;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;

namespace SOLTIUS_Web_API_Add_On.Database.Initializers
{
    public class SqlServerDatabaseInitializer : IDatabaseInitializer
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        public SqlServerDatabaseInitializer(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InitializeAsync(DBConfig config)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection(config);
            await connection.OpenAsync();

            await CreateSalesOrderHeader(connection);
            await CreateSalesOrderDetail(connection);
            await CreatePurchaseOrderHeader(connection);
            await CreatePurchaseOrderDetail(connection);
        }

        private async Task CreateSalesOrderHeader(SqlConnection connection)
        {
            string sql = @"
            IF OBJECT_ID('sales_order_header','U') IS NULL

            CREATE TABLE sales_order_header
            (
                id BIGINT NOT NULL IDENTITY(1,1),
                cardcode VARCHAR(30) NOT NULL,
                cardname VARCHAR(200) NOT NULL,
                docdate DATETIME NOT NULL,
                docduedate DATETIME NOT NULL,
                taxdate DATETIME NOT NULL,
                remarks VARCHAR(254),
                process_status TINYINT NOT NULL DEFAULT(0),
                retrycount INT NOT NULL DEFAULT(0),
                created_at DATETIME NOT NULL DEFAULT(GETDATE()),
                updated_at DATETIME NULL,
                processed_at DATETIME NULL,
                errormessage NVARCHAR(MAX) NULL
            );";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateSalesOrderDetail(SqlConnection connection)
        {
            string sql = @"
            IF OBJECT_ID('sales_order_detail','U') IS NULL

            CREATE TABLE sales_order_detail
            (
                id BIGINT NOT NULL IDENTITY(1,1),
                header_id BIGINT NOT NULL,
                linenum INT NOT NULL,
                itemcode VARCHAR(30) NOT NULL,
                itemname VARCHAR(200) NOT NULL,
                warehouse VARCHAR(20) NULL,
                quantity DECIMAL(19,6) NOT NULL,
                price DECIMAL(19,6) NULL,
                process_status TINYINT NOT NULL DEFAULT(0),
                retrycount INT NOT NULL DEFAULT(0),
                created_at DATETIME2 NOT NULL DEFAULT(GETDATE()),
                updated_at DATETIME NULL,
                processed_at DATETIME NULL,
                errormessage NVARCHAR(MAX) NULL
            );";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreatePurchaseOrderHeader(SqlConnection connection)
        {
            string sql = @"
            IF OBJECT_ID('SOL_PURCHASE_ORDER_HEADER','U') IS NULL
            CREATE TABLE SOL_PURCHASE_ORDER_HEADER
            (
                SOL_ID BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                SOL_CARDCODE VARCHAR(30) NOT NULL,
                SOL_CARDNAME VARCHAR(200) NOT NULL,
                SOL_DOCDATE DATETIME NOT NULL,
                SOL_DOCDUEDATE DATETIME NOT NULL,
                SOL_TAXDATE DATETIME NOT NULL,
                SOL_REMARKS VARCHAR(254),
                SOL_WEB_TX_NUMBER VARCHAR(50) NULL,
                SOL_WEB_TX_ID BIGINT NULL,
                SOL_UDF_DATA NVARCHAR(MAX) NULL,
                SOL_PROCESS_STATUS TINYINT NOT NULL DEFAULT(0),
                SOL_RETRYCOUNT INT NOT NULL DEFAULT(0),
                SOL_DOCENTRY VARCHAR(50),
                SOL_CREATED_AT DATETIME NOT NULL DEFAULT(GETDATE()),
                SOL_UPDATED_AT DATETIME NULL,
                SOL_PROCESSED_AT DATETIME NULL,
                SOL_ERRORMESSAGE NVARCHAR(MAX) NULL
            );

            IF COL_LENGTH('SOL_PURCHASE_ORDER_HEADER', 'SOL_WEB_TX_NUMBER') IS NULL
                ALTER TABLE SOL_PURCHASE_ORDER_HEADER ADD SOL_WEB_TX_NUMBER VARCHAR(50) NULL;
            IF COL_LENGTH('SOL_PURCHASE_ORDER_HEADER', 'SOL_WEB_TX_ID') IS NULL
                ALTER TABLE SOL_PURCHASE_ORDER_HEADER ADD SOL_WEB_TX_ID BIGINT NULL;
            IF COL_LENGTH('SOL_PURCHASE_ORDER_HEADER', 'SOL_UDF_DATA') IS NULL
                ALTER TABLE SOL_PURCHASE_ORDER_HEADER ADD SOL_UDF_DATA NVARCHAR(MAX) NULL;";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreatePurchaseOrderDetail(SqlConnection connection)
        {
            string sql = @"
            IF OBJECT_ID('SOL_PURCHASE_ORDER_DETAIL','U') IS NULL
            CREATE TABLE SOL_PURCHASE_ORDER_DETAIL
            (
                SOL_ID BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                SOL_HEADER_ID BIGINT NOT NULL,
                SOL_LINENUM INT NOT NULL,
                SOL_ITEMCODE VARCHAR(30) NOT NULL,
                SOL_ITEMNAME VARCHAR(200) NOT NULL,
                SOL_WAREHOUSE VARCHAR(20) NULL,
                SOL_QUANTITY DECIMAL(19,6) NOT NULL,
                SOL_PRICE DECIMAL(19,6) NULL,
                SOL_VAT_GROUP VARCHAR(20) NULL,
                SOL_WEB_LINE_ID BIGINT NULL,
                SOL_UDF_DATA NVARCHAR(MAX) NULL,
                SOL_PROCESS_STATUS TINYINT NOT NULL DEFAULT(0),
                SOL_RETRYCOUNT INT NOT NULL DEFAULT(0),
                SOL_CREATED_AT DATETIME2 NOT NULL DEFAULT(GETDATE()),
                SOL_UPDATED_AT DATETIME NULL,
                SOL_PROCESSED_AT DATETIME NULL,
                SOL_ERRORMESSAGE NVARCHAR(MAX) NULL
            );

            IF COL_LENGTH('SOL_PURCHASE_ORDER_DETAIL', 'SOL_VAT_GROUP') IS NULL
                ALTER TABLE SOL_PURCHASE_ORDER_DETAIL ADD SOL_VAT_GROUP VARCHAR(20) NULL;
            IF COL_LENGTH('SOL_PURCHASE_ORDER_DETAIL', 'SOL_WEB_LINE_ID') IS NULL
                ALTER TABLE SOL_PURCHASE_ORDER_DETAIL ADD SOL_WEB_LINE_ID BIGINT NULL;
            IF COL_LENGTH('SOL_PURCHASE_ORDER_DETAIL', 'SOL_UDF_DATA') IS NULL
                ALTER TABLE SOL_PURCHASE_ORDER_DETAIL ADD SOL_UDF_DATA NVARCHAR(MAX) NULL;";

            await connection.ExecuteAsync(sql);
        }
    }
}
