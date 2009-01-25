using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace CrudGenerator {
    public class Table {
        #region Member variables
        string tableName = string.Empty;
        string author = string.Empty;
        string isActive = string.Empty;
        List<Column> columns = new List<Column>();
        #endregion

        #region Properties
        public string Author {
            get { return author; }
            set { author = value; }
        }
        public string IsActive {
            get { return isActive; }
            set { isActive = value; }
        }
        public string TableName {
            get { return tableName; }
            set { tableName = value; }
        }
        public List<Column> Columns {
            get { return columns; }
            set { columns = value; }
        } 
        #endregion

        #region Column Selector Methods

        public List<Column> GetPrimaryKeys() {
            List<Column> list = new List<Column>();
            foreach (Column column in columns) {
                if (column.IsPrimaryKey)
                    list.Add(column);
            }
            return list;
        }
        public List<Column> GetNotPrimaryKeysAndNotIdentity() {
            List<Column> list = new List<Column>();
            foreach (Column column in columns) {
                if (!column.IsPrimaryKey && !column.IsIdentity)
                    list.Add(column);
            }
            return list;
        }
        public List<Column> GetAllColumns() {
            return columns;
        }
        public Column GetIdentityColumn() {
            foreach (Column column in columns) {
                if (column.IsIdentity)
                    return column;
            }
            return null;
        }
        public List<Column> GetNotIdentity() {
            List<Column> list = new List<Column>();
            foreach (Column column in columns) {
                if (!column.IsIdentity)
                    list.Add(column);
            }
            return list;
        } 
        #endregion

        #region STATIC - Parse Table method
        public static List<Table> ParseDataTable(DataTable dt) {
            List<Table> tables = new List<Table>();
            foreach (DataRow dr in dt.Rows) {
                Table table;
                if (tables.Count == 0 || tables[tables.Count - 1].TableName != dr["TableName"].ToString()) {
                    table = new Table();
                    table.TableName = dr["TableName"].ToString();
                    tables.Add(table);
                } else {
                    table = tables[tables.Count - 1];
                }
                Column column = new Column();
                column.Name = dr["ColumnName"].ToString();
                column.IsIdentity = ((int)dr["IsIdentity"] == 1);
                column.IsPrimaryKey = ((int)dr["IsPrimaryKey"] == 1);
                column.DataType = dr["DataType"].ToString();
                table.Columns.Add(column);
            }
            return tables;
        }
        #endregion

        #region CRUD Methods
        internal string GenerateSelectById() {
            StringBuilder sb = new StringBuilder(2000);
            WriteComments(sb);
            sb.Append("Create Procedure ");
            sb.Append(this.tableName);
            sb.Append("_ReadById\r\n");

            DeclareColumnList(sb, GetPrimaryKeys(), true);

            sb.Append("\r\nAS\r\nBegin\r\n\tSET NOCOUNT ON\r\n");
            sb.Append("\tselect\r\n\t");

            SelectColumns(sb, true, this.Columns);

            sb.Append("\r\n\tfrom ");
            sb.Append(this.tableName);

            PK_WhereClause(sb);

            sb.Append("\r\nEnd\r\n");

            return sb.ToString();
        }

        internal string GenerateSelectAll() {
            StringBuilder sb = new StringBuilder(2000);
            WriteComments(sb);
            sb.Append("Create Procedure ");
            sb.Append(this.tableName);
            sb.Append("_ReadAll\r\n");
            
            sb.Append("\r\nAS\r\nBegin\r\n\tSET NOCOUNT ON\r\n");
            sb.Append("\tselect\r\n\t");

            SelectColumns(sb, true, this.Columns);

            sb.Append("\r\n\tfrom ");
            sb.Append(this.tableName);
            sb.Append("\r\nEnd\r\n");

            return sb.ToString();
        }
        internal string GenerateUpdate() {
            StringBuilder sb = new StringBuilder(2000);
            WriteComments(sb);
            sb.Append("Create Procedure ");
            sb.Append(this.tableName);
            sb.Append("_Update\r\n");
            bool first;
            List<Column> notPrimaryOrIdentity = GetNotPrimaryKeysAndNotIdentity();

            first = DeclareColumnList(sb, GetPrimaryKeys(), true);
            DeclareColumnList(sb, notPrimaryOrIdentity, first);

            sb.Append("\r\nAS\r\nBegin\r\n\tSET NOCOUNT ON\r\n");
            sb.Append("\tupdate ");
            sb.Append(this.tableName);
            sb.Append("\r\n\tset\r\n");
            first = true;
            foreach (Column c in notPrimaryOrIdentity) {
                if (!first) {
                    sb.Append(",\r\n\t\t");
                } else {
                    first = !first;
                    sb.Append("\t\t");
                }
                sb.Append(c.Name);
                sb.Append(" = @");
                sb.Append(c.Name);
            }

            PK_WhereClause(sb);

            sb.Append("\r\nEnd\r\n");

            return sb.ToString();
        }

        internal string GenerateDelete() {
            StringBuilder sb = new StringBuilder(2000);
            WriteComments(sb);
            sb.Append("Create Procedure ");
            sb.Append(this.tableName);
            sb.Append("_Delete\r\n");

            DeclareColumnList(sb, GetPrimaryKeys(), true);

            sb.Append("\r\nAS\r\nBegin\r\n\tSET NOCOUNT ON\r\n");
            sb.Append("\tdelete from ");
            sb.Append(this.tableName);

            PK_WhereClause(sb);

            sb.Append("\r\nEnd\r\n");

            return sb.ToString();
        }

        internal string GenerateCreate() {
            StringBuilder sb = new StringBuilder(2000);
            WriteComments(sb);
            sb.Append("Create Procedure ");
            sb.Append(this.tableName);
            sb.Append("_Create\r\n");
            bool first;
            List<Column> nonIdentity = GetNotIdentity();

            first = DeclareColumnList(sb, nonIdentity, true);

            Column identity = GetIdentityColumn();

            if (identity != null) {
                DeclareColumn(sb, identity, first, true);
            }

            sb.Append("\r\nAS\r\nBegin\r\n\tSET NOCOUNT ON\r\n");
            sb.Append("\tinsert into ");
            sb.Append(this.tableName);
            sb.Append("\r\n\t\t(");

            SelectColumns(sb, true, nonIdentity);

            sb.Append(")\r\n\tvalues\r\n\t\t(");
            first = true;
            foreach (Column c in nonIdentity) {
                if (!first) {
                    sb.Append(",@");
                } else {
                    first = !first;
                    sb.Append("@");
                }
                sb.Append(c.Name);
            }
            sb.Append(")\r\n");
            if (identity != null) {
                sb.Append("\r\n\tselect @");
                sb.Append(identity.Name);
                sb.Append(" = SCOPE_IDENTITY()\r\n");
            }
            sb.Append("End\r\n");

            return sb.ToString();
        }
        internal string GenerateDeactiveate() {
            StringBuilder sb = new StringBuilder(2000);
            WriteComments(sb);
            sb.Append("Create Procedure ");
            sb.Append(this.tableName);
            sb.Append("_Deactivate\r\n");

            DeclareColumnList(sb, GetPrimaryKeys(), true);

            sb.Append("\r\nAS\r\nBegin\r\n\tSET NOCOUNT ON\r\n");
            sb.Append("\tupdate ");
            sb.Append(this.tableName);
            sb.Append("\r\n\tset\r\n\t\t");

            sb.Append(this.IsActive);

            sb.Append(" = 0");

            PK_WhereClause(sb);

            sb.Append("\r\nEnd\r\n");

            return sb.ToString();
        }
        #endregion

        #region CRUD - Common Code Methods
        private void WriteComments(StringBuilder sb) {
            sb.Append("-- =============================================");
            sb.Append("\r\n-- Author:\t\t");
            sb.Append(author);
            sb.Append("\r\n-- Create date:\t");
            sb.Append(DateTime.Now.ToShortDateString());
            sb.Append("\r\n-- Description:\t");
            sb.Append("\r\n-- Revisions:\t");
            sb.Append("\r\n-- =============================================\r\n");
        }
        private void PK_WhereClause(StringBuilder sb) {
            sb.Append("\r\n\twhere\r\n");
            bool first = true;
            foreach (Column c in GetPrimaryKeys()) {
                if (!first) {
                    sb.Append("\r\n\t\tand ");
                } else {
                    first = !first;
                    sb.Append("\t\t");
                }
                sb.Append(c.Name);
                sb.Append(" = @");
                sb.Append(c.Name);
            }
        }
        private bool DeclareColumnList(StringBuilder sb, List<Column> columns, bool first) {
            foreach (Column c in columns) {
                first = DeclareColumn(sb, c, first, false);
            }
            return first;
        }

        private static bool DeclareColumn(StringBuilder sb, Column c, bool first, bool output) {
            if (!first) {
                sb.Append(",\r\n");
            } else {
                first = !first;
            }
            sb.Append("\t@");
            sb.Append(c.Name + " ");
            sb.Append(c.DataType);
            if (output) {
                sb.Append(" OUTPUT");
            }
            return first;
        }
        private static bool SelectColumns(StringBuilder sb, bool first, List<Column> nonIdentity) {
            foreach (Column c in nonIdentity) {
                if (!first) {
                    sb.Append(", ");
                } else {
                    first = !first;
                    sb.Append(" ");
                }
                sb.Append(c.Name);
            }
            return first;
        }
        #endregion
    }
}
