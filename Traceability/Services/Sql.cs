using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

namespace Traceability.Services
{
    static public class Sql
    {
        public delegate void ConnectedDelegate(bool connected);
        public static event ConnectedDelegate ConnectionChange;
        private static bool LastConnectionState { get; set; }

        public static async void StartSQLConnectionMonitoring()
        {
            await Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var connection = CheckConnection();
                        
                        if (LastConnectionState.Equals(connection)) continue;
                        LastConnectionState = connection;
                        ConnectionChange.Invoke(connection);
                        
                        Task.Delay(15000).GetAwaiter().GetResult();
                    }
                }
                catch (Exception) { }
            });
        }
        private static bool CheckConnection() 
        {
            var commandText = "SELECT 1";
            try
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                    sqlCommand.CommandText = commandText;

                    sqlCommand.Connection.Open();

                    var result = sqlCommand.ExecuteScalar();

                    sqlCommand.Connection.Close();

                    return result.Equals(1);
                }
            }
            catch { return false; }
        }
        public static T SafeQueryInvoke<T>(Func<T> action, out bool success)
        {
            try 
            {
                var result = action();
                success = true;
                return result;
            }
            catch 
            {
                success = false;
                return default; 
            }
        }
        public static T SafeQueryInvoke<T>(Expression<Func<T>> action, Action<string> log, out bool success)
        {
            var methodName = ((MethodCallExpression)action.Body).Method.Name;
            try
            {
                var result = action.Compile().Invoke();
                success = true;
                log($"Успешно выполнен запрос {methodName}");
                return result;
            }
            catch (Exception ex)
            {
                success = false;
                log($"Ошибка запроса {methodName}\n {ex.Message}\n {ex.StackTrace}");
                return default;
            }
        }

        public static string CreateNewDummyNumber(string TSBarcode, string Product, string Side, int Line)
        {
            using (SqlCommand sqlCommand = new SqlCommand() { Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString()) })
            {
                sqlCommand.Connection.Open();

                sqlCommand.CommandText = "dbo.[sp_CreateNewDummyNumber]";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Add(new SqlParameter("@DUMMYNUM", SqlDbType.NVarChar, 13, ParameterDirection.Output, isNullable: false, 20, 0, "", DataRowVersion.Current, null));
                sqlCommand.Parameters.Add(new SqlParameter("@PRODUCT", SqlDbType.NVarChar, 10));
                sqlCommand.Parameters.Add(new SqlParameter("@SIDE", SqlDbType.NVarChar, 1));
                sqlCommand.Parameters.Add(new SqlParameter("@BARCODE", SqlDbType.NVarChar, 100));
                sqlCommand.Parameters.Add(new SqlParameter("@LINE", SqlDbType.Int, 1));
                sqlCommand.Parameters["@PRODUCT"].Value = Product;
                sqlCommand.Parameters["@SIDE"].Value = Side;
                sqlCommand.Parameters["@BARCODE"].Value = TSBarcode;
                sqlCommand.Parameters["@LINE"].Value = Line;
                sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                string result = sqlCommand.Parameters["@DUMMYNUM"].Value.ToString();

                return result;
            }
        }
        public static IEnumerable<MotorsCurrentPlan> GetMotorsPlan()
        {
            string commandText = "SELECT [product] ,[definition], [model], [count] " +
                "FROM [BekoLLCSQL].[dbo].[PRODUCTION_SHEDULE] where " +
                "date = CONVERT(date, GETDATE()) and " +
                "line = 2";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString()))
            {

                SqlCommand sqlCommand = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = CommandType.Text,
                    CommandText = commandText
                };
                connection.Open();
                SqlDataReader sqlData = sqlCommand.ExecuteReader();

                if (!sqlData.HasRows) yield return new MotorsCurrentPlan();

                while (sqlData.Read())
                {
                    yield return new MotorsCurrentPlan()
                    {
                        Product = sqlData["product"].ToString(),
                        Model = sqlData["definition"].ToString(),
                        Plan = (int)sqlData["count"]
                    };
                }
                connection.Close();
            }
        }
        public static string GetProductType(string productNo)
        {
            string commandText = $"SELECT TOP(1) [PRODUCTTYPE] FROM PRODUCT " +
                $"WHERE PRODUCT = '{productNo}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString().Trim() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }

        #region T_KKTS_FIXEDBARCODE

        public static string GetLeftDummy(string barcode)
        {
            string commandText = $"SELECT TOP(1) DUMMYNUML FROM T_KKTS_FIXEDBARCODE " +
                $"WHERE BARCODE = '{barcode}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetRightDummy(string barcode)
        {
            string commandText = $"SELECT TOP(1) DUMMYNUM FROM T_KKTS_FIXEDBARCODE " +
                $"WHERE BARCODE = '{barcode}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetProductNoByCS(string csBarcode)
        {
            string commandText = $"SELECT TOP(1) PRODUCT FROM T_KKTS_FIXEDBARCODE " +
                $"WHERE BARCODE = '{csBarcode}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static bool DoesExistInTkktsFixedBarcodeByBarcode(string componentBarcode, string rdummy)
        {
            string commandText = $"IF EXISTS (" +
                $"SELECT TOP(1) * FROM [BEKOLLCSQL].[DBO].[T_KKTS_FIXEDBARCODE] " +
                $"WHERE BARCODE = '{componentBarcode}' AND " +
                $"DUMMYNUM = '{rdummy}'" +
                $")" +
                "SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static bool DoesExistInTkktsFixedBarcodeByLocation(string productBarcode, string rdummy, int location)
        {
            string commandText = $"IF EXISTS (" +
                $"SELECT TOP(1) * FROM [BEKOLLCSQL].[DBO].[T_KKTS_FIXEDBARCODE] " +
                $"WHERE LOCATION = '{location}' AND " +
                $"DUMMYNUM = '{rdummy}'" +
                $") " +
                "SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static int AddToTkktsFixedBarcode(Models.BekoProduct product, Models.BekoComponent component, int location)
        {
            string commandText = $"INSERT INTO [DBO].[T_KKTS_FIXEDBARCODE] VALUES (" +
                $"'{product.WMDetails.RDummy}'," +
                $"'{product.WMDetails?.LDummy}'," +
                $"''," + // RFIDTAG
                $"'{component.ComponentBarcode}'," +
                $"'{product.ProductNo}'," +
                $"'2'," +
                $"'{location}'," +
                $"NULL," +
                $"GETDATE()" +
                $")";
            try
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                    sqlCommand.CommandText = commandText;

                    sqlCommand.Connection.Open();

                    var result = sqlCommand.ExecuteNonQuery();

                    sqlCommand.Connection.Close();

                    return result;
                }
            }
            catch (Exception) { return -1; }
        }
        public static int UpdateTkktsFixedBarcode(Models.BekoProduct product, Models.BekoComponent component, int location)
        {
            string commandText = $"UPDATE [DBO].[T_KKTS_FIXEDBARCODE] SET " +
                $"BARCODE = '{component.ComponentBarcode}', " +
                "SYSDATE = GETDATE() " +
                "WHERE " +
                $"DUMMYNUM = '{product.WMDetails.RDummy}' AND " +
                $"LOCATION = '{location}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static int GetQuantityOfSavedMTKComponentsWM(string MTK)
        {
            string commandText = $"SELECT COUNT(BARCODE) FROM [BEKOLLCSQL].[DBO].[T_KKTS_FIXEDBARCODE] " +
                $"WHERE BARCODE = '{MTK}' AND " +
                $"SYSDATE > CAST(GETDATE() - 5 AS DATE)";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var preResult = sqlCommand.ExecuteScalar();

                var result = Convert.ToInt32(preResult);

                sqlCommand.Connection.Close();
                return result;
            }
        }
        #endregion

        #region KKI_COMPONENTS

        public static string GetLocationNameByStation(int? station)
        {
            string commandText = $"SELECT TOP (1) [COMPONENTNAME] FROM [BekoLLCSQL].[dbo].[KKI_COMPONENTS] " +
                $"WHERE STATION = '{station}'";
            
            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetComponentCodeByStation(int? station)
        {
            string commandText = $"SELECT TOP (1) [COMPONENTCODE] FROM [BekoLLCSQL].[dbo].[KKI_COMPONENTS] " +
                $"WHERE STATION = '{station}'";
            
            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        #region SAP_Components dbo

        public static IEnumerable<string> GetSuitableMotors(string StockNo)
        {
            string commandText = $"SELECT [COMPONENT_CODE] FROM [BekoLLCSQL].[dbo].[T_KKTS_SAP_COMPONENTS] where PRODUCT = '{StockNo}' and DISPO in (525,528)";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString()))
            {

                SqlCommand sqlCommand = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = CommandType.Text,
                    CommandText = commandText
                };
                connection.Open();

                SqlDataReader sqlData = sqlCommand.ExecuteReader();

                if (!sqlData.HasRows) yield return string.Empty;

                while (sqlData.Read()) yield return sqlData["COMPONENT_CODE"].ToString();

                connection.Close();
            }
        }
        public static string GetProductMaterials(string productNo)
        {
            var MRP = ConfigurationManager.AppSettings["MRP"];

            string commandText = $"SELECT [COMPONENT_CODE] FROM [BEKOLLCSQL].[DBO].[T_KKTS_SAP_COMPONENTS] " +
                $"WHERE PRODUCT = '{productNo}' " +
                $"{(string.IsNullOrEmpty(MRP) ? "" : $"AND DISPO IN ({MRP})")}" +
                $"GROUP BY COMPONENT_CODE";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var datareader = sqlCommand?.ExecuteReader();

                string result = "";

                if (datareader.HasRows)
                    while(datareader.Read())
                        result += datareader.GetValue(0) + ", ";

                sqlCommand.Connection.Close();

                return String.IsNullOrEmpty(result) ? result : result.Remove(result.LastIndexOf(','), 2);
            }
        }
        public static string GetMaterialModel(string material)
        {
            string commandText = $"SELECT [COMPONENT_NAME] FROM [BEKOLLCSQL].[DBO].[T_KKTS_SAP_COMPONENTS] " +
                $"WHERE COMPONENT_CODE = '{material}' ";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetTrueMaterial(string[] materials)
        {
            string commandText = $"SELECT TOP (1) [COMPONENT_CODE] FROM [BEKOLLCSQL].[DBO].[T_KKTS_SAP_COMPONENTS] " +
                $"WHERE COMPONENT_CODE in ('{string.Join("','",materials)}') ";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        #region KKTS_PPlan dbo

        public static int CreateNewTempMotorsPPlan(MotorsCurrentPlan plan, string BROperator = "")
        {
            string commandText =

                $"UPDATE [DBO].[T_KKTS_PPLAN] " +
                $"SET " +
                $"STATUS = 8, " +
                $"UPDATETIME = GETDATE() " +
                $"WHERE STATION = 102 AND " +
                $"STATUS = 1 AND " +
                $"SIDE = 'L' " +

                $"UPDATE [DBO].[T_KKTS_PPLAN] " +
                $"SET " +
                $"STATUS = 2" +
                $"WHERE STATION = 102 AND " +
                $"ACTUAL = TARGET " +

                $"INSERT INTO [DBO].[T_KKTS_PPLAN] VALUES (" +
                $"'L', " +
                $"2, " +
                $"'{plan.Product}', " +
                $"'{plan.Plan}', " +
                $"1, " +
                $"0, " +
                $"'{BROperator}', " +
                $"GETDATE(), " +
                $"GETDATE(), " +
                $"'102'" +
                $") ";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static int UpdateTempMotorsPPlan(MotorsCurrentPlan plan)
        {
            var actual = GetProductsQtyInLeftSide(plan);
            var targetPlan = plan.Plan;
            var product = plan.Product;

            var doesPlanComplete = actual >= targetPlan;

            var commandText =
                $"UPDATE [DBO].[T_KKTS_PPLAN] SET " +
                $"{(doesPlanComplete ? "STATUS = 2," : "")}" +

                $"ACTUAL = {actual}, " +
                $"TARGET = {targetPlan}, " +
                $"UPDATETIME = GETDATE() " +

                $"WHERE PRODUCT = '{product}' AND " +
                $"STATUS = 1 AND " +
                $"SIDE = 'L'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static bool DoesExistsInTkktsPPlan(string product, int line)
        {
            string commandText = $"IF EXISTS(SELECT * FROM [dbo].[T_KKTS_PPLAN] where " +
                $"[PRODUCT] = '{product}' AND" +
                $"[LINE] = {line} AND " +
                $"[STATUS] = 1) SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static MotorsCurrentPlan GetActualProductMotorsPPlan()
        {
            string commandText = "SELECT top (1) [PRODUCT], [TARGET], [SYSDATE] FROM [BekoLLCSQL].[dbo].[T_KKTS_PPLAN] where " +
                "STATUS = 1 AND " +
                "STATION = 102 AND " +
                "SIDE = 'L'";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString()))
            {

                SqlCommand sqlCommand = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = CommandType.Text,
                    CommandText = commandText
                };

                connection.Open();

                SqlDataReader sqlData = sqlCommand.ExecuteReader();

                if (!sqlData.HasRows) return new MotorsCurrentPlan();

                var result = new MotorsCurrentPlan();

                while (sqlData.Read())
                {
                    result = new MotorsCurrentPlan()
                    {
                        Product = sqlData["PRODUCT"].ToString(),
                        Model = "-",
                        Plan = (int)sqlData["TARGET"],
                        SysDate = (DateTime)sqlData["SYSDATE"],
                        SuitableMotors = GetSuitableMotors(sqlData["PRODUCT"].ToString()).ToArray()
                    };
                }
                connection.Close();

                return result;
            }
        }
        #endregion

        #region RevokeComponents dbo

        public static int AddToRevokeComponents(string barcode, int line, string BROperator = "")
        {
            var material = barcode.Substring(0, 10);

            string commandText = $"INSERT INTO [DBO].[REVOKE_COMPONENTS] VALUES (" +
                $"'{material}'," +
                $"(SELECT TOP(1) COMPONENT_CODE FROM [DBO].[REVOKE_COMPONENTS] WHERE MATERIAL = '{material}')," +
                $"(SELECT TOP(1) COMPONENT_NAME FROM [DBO].[REVOKE_COMPONENTS] WHERE MATERIAL = '{material}')," +
                $"'{barcode}'," +
                $"0," +
                $"NULL," +
                $"(SELECT TOP(1) COMPONENT_DISCRIPTION FROM [DBO].[REVOKE_COMPONENTS] WHERE MATERIAL = '{material}')," +
                $"{line}," +
                $"(SELECT TOP(1) MRP_CODE FROM [DBO].[REVOKE_COMPONENTS] WHERE MATERIAL = '{material}')," +
                $"Getdate()," +
                $"'{BROperator}'" +
                $")";
            try
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                    sqlCommand.CommandText = commandText;

                    sqlCommand.Connection.Open();

                    var result = sqlCommand.ExecuteNonQuery();

                    sqlCommand.Connection.Close();

                    return result;
                }
            }
            catch (Exception) { return -1; }
        }
        public static bool DoesExistsInRevokeComponents(string barcode)
        {
            string commandText = $"IF EXISTS(SELECT * FROM [dbo].[REVOKE_COMPONENTS] where [COMPONENT_BARCODE] = '{barcode}') SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        #region LeftSide dbo

        public static int AddToTkktsLeftside(string dummy, string barcode, string product, int location)
        {
            string commandText = $"INSERT INTO [DBO].[T_KKTS_LEFTSIDE] VALUES (" +
                $"'{dummy}', " +
                $"'', " +
                $"'{barcode}', " +
                $"'{product}', " +
                $"2, " +
                $"'{location}', " +
                $"GETDATE()" +
                $")";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static bool DoesExistsInLeftside(string dummy)
        {
            string commandText = $"IF EXISTS (SELECT [DUMMYNUML] FROM [BEKOLLCSQL].[DBO].[T_KKTS_LEFTSIDE]" +
                $"WHERE [DUMMYNUML] = '{dummy}')" + 
                "SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static int GetProductsQtyInLeftSide(MotorsCurrentPlan plan)
        {
            string commandText = $"SELECT COUNT(*) FROM [BekoLLCSQL].[dbo].[T_KKTS_LEFTSIDE] WHERE " +
                $"PRODUCT = '{plan.Product}' AND " +
                $"SYSDATE > " +
                $"'{(plan.SysDate == DateTime.MinValue ? DateTime.Now : plan.SysDate): yyyy-MM-dd HH:mm:ss}' AND " +
                $"LOCATION = 102";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var preResult = sqlCommand.ExecuteScalar();

                var result = Convert.ToInt32(preResult);

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        public static bool CompareTubAndComponent(string tub, string component)
        {
            string commandText = $"IF EXISTS" +
                $"(" +

                    $"SELECT DUMMYCOMPARE.BARCODE, LEFTSIDE.BARCODE " + 
                    $"FROM T_KKTS_LEFTSIDE AS LEFTSIDE " +
                    $"RIGHT JOIN T_KKTS_DUMMYCOMPARE AS DUMMYCOMPARE " +
                    $"ON LEFTSIDE.DUMMYNUML = DUMMYCOMPARE.DUMMYNUM " +
                    $"WHERE " +
                    $"DUMMYCOMPARE.BARCODE = '{tub}' AND " +
                    $"LEFTSIDE.BARCODE = '{component}' " +

                $") SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }

        #region DummyCompare dbo

        public static string GetProductFromDummyCompare(string barcode)
        {
            string commandText = $"SELECT TOP(1) [PRODUCT] FROM [dbo].[T_KKTS_DUMMYCOMPARE] " +
                $"where [BARCODE] = '{barcode}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetLeftDummyFromDummyCompare(string barcode)
        {
            string commandText = $"SELECT TOP(1) [DUMMYNUM] FROM [dbo].[T_KKTS_DUMMYCOMPARE] " +
                $"where [BARCODE] = '{barcode}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetTypeOfIOTByWMDummy(string dummy)
        {
            string sapQuery = $"SELECT TOP (1) PRODUCT FROM T_KKTS_DUMMYCOMPARE WHERE " +
                $"DUMMYNUM = '{dummy}'";

            string sapqueryResult = "";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = sapQuery;

                sqlCommand.Connection.Open();

                sapqueryResult = sqlCommand.ExecuteScalar()?.ToString() ?? "not found";

                sqlCommand.Connection.Close();
            }

            if (string.IsNullOrEmpty(sapqueryResult)) return "-";
            return GetTypeOfIOT(sapqueryResult);
        }
        public static bool DoesExistsInDummyCompare(string tubBarcode)
        {
            string commandText = $"IF EXISTS(SELECT * FROM [dbo].[T_KKTS_DUMMYCOMPARE] " +
                $"where [BARCODE] = '{tubBarcode}') " +
                $"SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        #region T_ATR_VALUE_TR_TR dbo

        public static string GetTypeOfIOT(string SKU)
        {
            string commandText = $"SELECT TOP (1) [ATR_VALUE_MDM] FROM [ETIKET].[DBO].[T_ATR_VALUE_TR_TR] " +
                $"WHERE " +
                $"DESCRIPTION = 'CONNECTIVITY' AND " +
                $"SKUNUMBER = '{SKU}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStrEtiket"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand?.ExecuteScalar()?.ToString();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static string GetProductBrandMarketingCode(string SKU)
        {
            string commandText = $"SELECT TOP (2) [ATR_VALUE] FROM [ETIKET].[DBO].[T_ATR_VALUE_TR_TR] " +
                $"WHERE SKUNUMBER = '{SKU}' AND " +
                $"(DESCRIPTION = 'BRAND' OR " +
                $"DESCRIPTION = 'MARKETING CODE')";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var datareader = sqlCommand?.ExecuteReader();

                string result = "";

                if (datareader.HasRows)
                    while(datareader.Read())
                        result += datareader.GetValue(0) + " ";

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        #region KKI_Match dbo

        public static int GetQuantityOfSavedMTKComponentsREF(string MTK)
        {
            string commandText = $"SELECT COUNT(BARCODE) FROM [BEKOLLCSQL].[DBO].[KKI_MATCH] " +
                $"WHERE BARCODE = '{MTK}' AND " +
                $"SYSDATE > CAST(GETDATE() - 5 AS DATE)";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var preResult = sqlCommand.ExecuteScalar();

                var result = Convert.ToInt32(preResult);

                sqlCommand.Connection.Close();
                return result;
            }
        }
        public static bool DoesExistInKKIMatchByBarcode(string productBarcode, string componentBarcode)
        {
            string commandText = $"IF EXISTS (" +
                $"SELECT TOP(1) * FROM [BEKOLLCSQL].[DBO].[KKI_MATCH] " +
                $"WHERE BARCODE = '{componentBarcode}' AND " +
                $"PRODUCT = '{productBarcode.Substring(0, 10)}' AND " +
                $"SERIAL = '{productBarcode.Substring(10, 12)}'" +
                $")" +
                "SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static bool DoesExistInKKIMatchByComponentCode(string productBarcode, string componentCode)
        {
            string commandText = $"IF EXISTS (" +
                $"SELECT TOP(1) * FROM [BEKOLLCSQL].[DBO].[KKI_MATCH] " +
                $"WHERE COMPONENTCODE = '{componentCode}' AND " +
                $"PRODUCT = '{productBarcode?.Substring(0, 10)}' AND " +
                $"SERIAL = '{productBarcode?.Substring(10, 12)}'" +
                $") " +
                "SELECT 1 ELSE SELECT 0";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = Convert.ToBoolean(sqlCommand.ExecuteScalar());

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static int AddToKKIMatch(string productBarcode, Models.BekoComponent component)
        {
            string commandText = $"INSERT INTO [DBO].[KKI_MATCH] VALUES (" +
                $"'{component.ComponentCode}', " +
                $"'{productBarcode?.Substring(0, 10)}', " +
                $"'{productBarcode?.Substring(10, 12)}', " +
                $"'{component.ComponentMaterial}', " +
                $"'{component.ComponentBarcode}', " +
                $"'{component.ComponentMaterialModel}', " +
                $"'', " +
                $"GETDATE()" +
                $")";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        public static int UpdateKKIMatch(string productBarcode, Models.BekoComponent component)
        {
            string commandText = $"UPDATE [DBO].[KKI_MATCH] SET " +
                $"MATERIAL = '{component.ComponentMaterial}', " +
                $"BARCODE = '{component.ComponentBarcode}', " +
                $"MODEL = '{component.ComponentMaterialModel}', " +
                "SYSDATE = GETDATE() " +
                "WHERE " +
                $"PRODUCT = '{productBarcode.Substring(0, 10)}' AND " +
                $"SERIAL = '{productBarcode.Substring(10, 12)}' AND " +
                $"COMPONENTCODE = '{component.ComponentCode}'";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var result = sqlCommand.ExecuteNonQuery();

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion

        #region not used

        public static int GetProductsQtyInFixedBarcode(string product)
        {
            string commandText = $"SELECT COUNT(*) FROM [BekoLLCSQL].[dbo].[T_KKTS_FIXEDBARCODE] WHERE " +
                $"PRODUCT = '{product}' AND " +
                $"SYSDATE > '{DateTime.Now.Date:yyyy-MM-dd}' AND " +
                $"LOCATION = 102";

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

                sqlCommand.CommandText = commandText;

                sqlCommand.Connection.Open();

                var preResult = sqlCommand.ExecuteScalar();

                var result = Convert.ToInt32(preResult);

                sqlCommand.Connection.Close();

                return result;
            }
        }
        #endregion
    }
}
