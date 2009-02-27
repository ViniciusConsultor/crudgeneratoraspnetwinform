﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using u = CrudGenerator.Util.Utility;

namespace CrudGenerator
{
    /// <summary>
    /// What this class needs to do is look through a list of columns and 
    /// </summary>
    class CrudGenCSharp
    {

        StringBuilder crudObject=new StringBuilder();
        StringBuilder crudData=new StringBuilder();
        /// <summary>Data Access Layer</summary>
        StringBuilder crudDAL=new StringBuilder();
        string _namespace;
        /// <summary>synonymous with the tableName</summary>
        string _className;
        public string ClassName{get { return _className; }}

        List<Column> _cols;
        public string CrudObject { get { return crudObject.ToString(); } }
        public string CrudData { get { return crudData.ToString(); } }

        public CrudGenCSharp() { }
        public CrudGenCSharp(string nameSpace, string className, List<Column> cols)
        {
            _namespace=nameSpace;
            _className = className.Substring(0, 1).ToUpper() + className.Substring(1);
            _cols = cols;
            
            BuildCrudObject();
            BuildCrudData();
            
           
        
        }

      
        public void BuildCrudObject(){
            crudObject.AppendFormat("{1}//******************** {0} ****************************//{1}", _className, Environment.NewLine );
            crudObject.AppendFormat("\r\n namespace {0} {{\r\n", _namespace);
            crudObject.AppendFormat("public class {0}{{\r\n", _className );
            crudObject.Append(BuildFields(_cols));
            crudObject.Append(BuildProps(_cols) );
            crudObject.Append(BuildConstructor(_cols, _className));
            crudObject.Append(BuildCRUD(_cols));
            crudObject.AppendFormat("}} //{0}\r\n", _className);
            crudObject.AppendFormat("\r\n}} //{0}", _namespace);
        }

        private void BuildCrudData() {
            crudData.AppendFormat("namespace {0} {{\r\n", _namespace);
            crudData.AppendFormat("public class {0}Data{{\r\n", _className);
            crudData.Append(BuildCrud_CreateDL(_cols));
            crudData.AppendFormat("}} //{0}\r\n", _className);
            crudData.AppendFormat("\r\n}} //{0}\r\n", _namespace);
        }

        private void BuildCrudDAL(){

        
        }


        private string BuildFields(List<Column> cols) {
            System.ComponentModel.TypeConverter tc = new System.ComponentModel.TypeConverter();

            string result="#region Fields\r\n";
            foreach (Column c in cols) {
                //private int _field;
                result += string.Format( "\t private {0} {1};\r\n", 
                    c.GetASPNetDataType(),GetFieldName(c));
            }
            result += "#endregion //Fields\r\n";

            return result;
        }

        private string BuildProps(List<Column> cols)
        {
            System.ComponentModel.TypeConverter tc = new System.ComponentModel.TypeConverter();

            string result = "#region Props\r\n";

            foreach (Column c in cols)
            {
                //public int Property {get{return _field;} set{_field=value;}}
                result += string.Format("\tpublic {0} {2} {{get{{return {1};}} set{{{1}=value;}}}}\r\n",
                    c.GetASPNetDataType(), GetFieldName(c), GetPropName(c));
            }
            result += "#endregion //Props\r\n";

            return result;
        }

        private string BuildConstructor(List<Column> cols, string className)
        {
            System.ComponentModel.TypeConverter tc = new System.ComponentModel.TypeConverter();

            string result = "#region CTOR\r\n";
            string CTORdflt=string.Format("\tpublic {0}(){{\r\n", className);
            string CTOR = string.Format("\tpublic {0} ({1}){{\r\n", className, GetFieldsAsInputParams(cols));
            foreach (Column c in cols)
            {
                
                //_field=value;
                CTORdflt += string.Format("\t\tpublic {0} {2} {{get{{return {1};}} set{{{1}=value}}}};\r\n",
                    c.GetASPNetDataType(), GetFieldName(c), GetPropName(c));

                //_field=inputParam;
                CTOR += string.Format ("\t\t{0}={1};\r\n",
                    GetFieldName(c), GetInputParamName(c));

            }
            

            CTORdflt += "\t}\r\n";
            CTOR += "\t}\r\n";
            result += CTORdflt + CTOR;
            result += "#endregion //CTOR\r\n";
            return result;
        }

        private string BuildCRUD(List<Column> cols) {
            StringBuilder result= new StringBuilder().Append("#region CRUD\r\n");
            string pattern = "public bool ACTION(PARAMS){\r\n//TODO setup ACTION\r\n}\r\n";
            string create, retrieveByID,retrieveAll, update, delete;
            create = retrieveByID = retrieveAll = update = delete = pattern;
            create = BuildCrud_CreateBL(cols);
            retrieveByID = pattern.Replace("ACTION", "RetrieveByID").Replace("PARAMS", GetFieldsAsInputParams(cols, true));
            retrieveAll = pattern.Replace("ACTION", "RetrieveAll").Replace("PARAMS", "");
            update = pattern.Replace("ACTION", "Update").Replace("PARAMS", "");
            delete = pattern.Replace("ACTION", "Delete").Replace("PARAMS", "");
            result.Append(create);
            result.Append(retrieveAll);
            result.Append(retrieveByID);
            result.Append(update);
            result.Append(delete);
            result.Append("#endregion //CRUD\r\n");
            return result.ToString();
        }

        /// <summary>Create should return the datatype of the current </summary>
        private string BuildCrud_CreateBL(List<Column> cols)
        {
            string result = "";
            //get the first key column's data type and use it as the return type 
            string primaryKeyDataType = GetFirstKeyColumn(cols).GetASPNetDataType()  ;
            result = "///<summary>returns the id of the item which was just created</summary>"
                + string.Format("public {0} Create({1}){{\r\n", primaryKeyDataType, (u.UserSettings.UserIdIsParamForCRUBusinessLayer)?"Guid userId":"" )
                + string.Format("\t return {0}Data.Create({1}this);\r\n", _className, (u.UserSettings.UserIdIsParamForCRUBusinessLayer) ? "userId, " : "")
                + "}}";
            return result;
        }

        /// <summary>Create should return the datatype of the current </summary>
        private string BuildCrud_CreateDL(List<Column> cols)
        {
            string result = "";
            //todo referenct the create stored procedure and pass to it parameters to create a new item and return id of the new entry
            Column pkCol = GetFirstKeyColumn(cols);
            result = "///<summary>returns the id of the item which was just created</summary>\r\n"
                + string.Format("public static {0} Create({1}{2} inputObj){{\r\n", pkCol.GetASPNetDataType(),
                    (u.UserSettings.UserIdIsParamForCRUBusinessLayer) ? "Guid userId," : "", _className)
                + string.Format("\t using(SqlCommand cmd=new SqlCommand(\"{0}\")){{\r\n", CrudGenSPROC.GetSprocNameCreate(_className))
                + "\t\t cmd.CommandType = CommandType.StoredProcedure;\r\n";

            //for each column, add a parameter to the sproc
            foreach (Column c in cols) {
                result += string.Format("\t\t cmd.Parameters.AddWithValue(\"@{0}\" , inputObj.{1})\r\n", c.Name, GetPropName(c));
                //todo: convert the value from theinput object to database friendly value... this means need to have a ToDBFriendly method
            }
            //todo: create DataAccessLayer which runs a command and returns each type of data - most important for me is int and unique identifier, and boolean
            result += string.Format("\t\t return DataAccessLayer.RunCmdReturn_{0}(cmd);\r\n", pkCol.GetASPNetDataType());

           result += string.Format("\t }} //close using statement \r\n")
                + "}";
            return result;
        }
        private Column GetFirstKeyColumn(List<Column> cols) {
            Column keyCol = null;
            foreach (Column c in cols) {
                if (c.IsIdentity)
                {
                    keyCol = c;
                    break;
                }
            }

            if (keyCol == null) keyCol = cols[0];
            return keyCol;
        }
        private string GetFieldsAsInputParams(List<Column> cols) {
            return GetFieldsAsInputParams(cols, false);
        }
        private string GetFieldsAsInputParams(List<Column> cols, bool onlyKeyCols){
            string result = "";
            foreach (Column c in cols) {
                if (result.Length > 0) result += ",";
                if(onlyKeyCols && c.IsIdentity)
                    result += GetFieldAsInputParameter(c);
                else
                    result += GetFieldAsInputParameter(c);

            }

            return result;
        
        }
        private string GetFieldAsInputParameter(Column c) {
            string result = "";
            result = string.Format ("{0} {1}",
                c.GetASPNetDataType(), GetInputParamName(c));
            return result;
        
        }
        

        private string GetInputParamName(Column c) {
            return c.Name + "in";
        }
        private string GetFieldName(Column c) {
            string result = (c.Name.Length > 1)? 
                string.Format("_{0}{1}", c.Name.Substring(0,1).ToLower(), c.Name.Substring(1)):
                "_" + c.Name.ToLower();
            return result;

        }
        private string GetPropName(Column c) {
            string result = (c.Name.Length > 1) ?
                string.Format("{0}{1}", c.Name.Substring(0, 1).ToUpper(), c.Name.Substring(1)) :
                c.Name.ToUpper(); 
            return result;
        }

        /// <summary>
        /// The initial value fields are set to when the constructor is invoked
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetInitialValueByType(Column c) {
            SqlDbType type = (SqlDbType)Enum.Parse(typeof(SqlDbType), c.DataType);
            string result = "\"\"";
            switch (type) { 
                case SqlDbType.BigInt :
                case SqlDbType.Binary:
                case SqlDbType.Bit:               
                case SqlDbType.Decimal:
                case SqlDbType.Float:
                case SqlDbType.Int :
                case SqlDbType.Money:
                case SqlDbType.Real:
                case SqlDbType.SmallInt:
                case SqlDbType.SmallMoney:
                case SqlDbType.TinyInt:
                case SqlDbType.VarBinary:
                    result = "0";break;
                case SqlDbType.Char:
                case SqlDbType.NChar:
                    result = "''";break;
                case SqlDbType.VarChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                    result = "\"\"";break;
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                case SqlDbType.Time:
                    result = "null";break;
                case SqlDbType.UniqueIdentifier:
                    result = "new Guid()"; break;
            }
                
            return result;
        }

        public static string GetDataAccessLayer() {
            return "";
        }
        /// <summary>
        /// Helps to reduce the amount of code it takes to extract data from a dataReader
        /// </summary>
        /// <returns></returns>
        public static string GetDataReaderExtensions() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Web;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Data.SqlClient;");
            sb.AppendFormat ("namespace {0}.DL {{\r\n", u.UserSettings.CodeNamespace );
            sb.AppendLine("    public static class DataReaderExtensions {");
            sb.AppendLine("        public static string ToString(this IDataReader reader, string column) {");
            sb.AppendLine("            if (reader[column] != DBNull.Value)");
            sb.AppendLine("                return reader[column].ToString();");
            sb.AppendLine("            else");
            sb.AppendLine("                return \"\";");
            sb.AppendLine("        }");
            sb.AppendLine("        public static Boolean ToBool(this IDataReader reader, string column, bool defaultValue)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader[column] != DBNull.Value)");
            sb.AppendLine("                return bool.Parse(reader[column].ToString());");
            sb.AppendLine("            else");
            sb.AppendLine("                return defaultValue;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public static int ToInt(this IDataReader reader, string column)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader[column] != DBNull.Value)");
            sb.AppendLine("            {");
            sb.AppendLine("                return Convert.ToInt32(reader[column]);");
            sb.AppendLine("            }");
            sb.AppendLine("            else                ");
            sb.AppendLine("                return 0;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public static Decimal ToDecimal(this IDataReader reader, string column)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader[column] != DBNull.Value)");
            sb.AppendLine("            {");
            sb.AppendLine("                return Convert.ToDecimal(reader[column]);");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("                return 0;");
            sb.AppendLine("        }");
            sb.AppendLine("        public static Guid ToGuid(this IDataReader reader, string column)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader[column] != DBNull.Value)");
            sb.AppendLine("            {");
            sb.AppendLine("                return new Guid(reader[column].ToString());");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("                return Guid.Empty;");
            sb.AppendLine("        }");
            sb.AppendLine("        public static DateTime ToDateTime(this IDataReader reader, string column)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader[column] != DBNull.Value)");
            sb.AppendLine("            {");
            sb.AppendLine("                return Convert.ToDateTime(reader[column]);");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("                return DateTime.MinValue;");
            sb.AppendLine("        }");
            sb.AppendLine("        //This converts an integer column to the given enum (T)");
            sb.AppendLine("        public static T ToEnum<T>(this IDataReader reader, string column)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (!typeof(T).IsEnum)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw new ArgumentException(typeof(T).ToString() + \" is not an Enum\");");
            sb.AppendLine("            }");
            sb.AppendLine("            return (T)Enum.ToObject(typeof(T), reader.ToInt(column));");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("} //end namespace");
            return sb.ToString();
        }
       
    }
}