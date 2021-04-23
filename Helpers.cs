using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Net;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Web.Script.Serialization;

namespace Utilities
{
    public static class Helpers{

        public static SqlCommand BuildCommand(string Proc){
            //Build Stored Procedure Command
            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = Proc;

            return objCommand;
        }

        
        public static SqlCommand BuildCommand(string Proc, string InputParam, object Input){
            //Build Stored Procedure Command
            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = Proc;
            objCommand.Parameters.AddWithValue(InputParam, Input);

            return objCommand;
        }


        public static SqlCommand BuildCommand(string Proc, string[] InputParams, object[] Inputs){
            //Build Stored Procedure Command
            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = Proc;

            int i = 0;
            foreach (string param in InputParams){

                //Add coresponding Parameter and Value
                objCommand.Parameters.AddWithValue(InputParams[i], Inputs[i]);

                //Increment Param
                i++;
            }

            return objCommand;
        }

        public static SqlCommand BuildCommand(string Proc, List<string> InputParams, List<object> Inputs) {
            //Build Stored Procedure Command
            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = Proc;

            int i = 0;
            foreach (string param in InputParams) {

                //Add coresponding Parameter and Value
                objCommand.Parameters.AddWithValue(InputParams[i], Inputs[i]);

                //Increment Param
                i++;
            }

            return objCommand;
        }


        public static SqlCommand BuildCommand(string Proc, Dictionary<string, object> Params) {
            //Build Stored Procedure Command
            SqlCommand objCommand = new SqlCommand();
            objCommand.CommandType = CommandType.StoredProcedure;
            objCommand.CommandText = Proc;
            
            foreach (KeyValuePair<string, object> param in Params) {

                //Add coresponding Parameter and Value
                objCommand.Parameters.AddWithValue(param.Key, param.Value);
            }

            return objCommand;
        }



        public static DataSet ExecuteFetch(SqlCommand command){

            //Open Connection
            DBConnect newConnection = new DBConnect();

            //Execute Command
            DataSet Data = newConnection.GetDataSetUsingCmdObj(command);

            //Close DB Connection
            newConnection.CloseConnection();

            return Data;
        }


        public static void ExecuteUpdate(SqlCommand command){

            //Open Connection
            DBConnect newConnection = new DBConnect();

            //Execute Command
            newConnection.DoUpdateUsingCmdObj(command);

            //Close DB Connection
            newConnection.CloseConnection();
        }

        
        public static List<object> CallAPI(string APIurl){
            
            // Make call to DB
            WebRequest request = WebRequest.Create(APIurl);
            WebResponse response = request.GetResponse();

            // Read DB response
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            String data = reader.ReadToEnd();
            reader.Close();
            response.Close();

            //Translate DB response
            JavaScriptSerializer jss = new JavaScriptSerializer();
            List<object> apiResponseList = jss.Deserialize<List<object>>(data);

            return apiResponseList;
            
        }



        public static class TypeTools{

            public static Y NewType<Y>() where Y : new(){
                return new Y();
            }
        }





        public static class DataSets {


            public static bool IsValid(DataSet ds) {

                //if ds does not contain DataRows
                if (ds == null
                     || ds.Tables == null
                     || ds.Tables.Count < 1
                     || ds.Tables[0] == null
                     || ds.Tables[0].Rows == null
                     || ds.Tables[0].Rows.Count < 1) {

                    return false;
                }
                return true;
            }


            public static DataRow ExtractRow(DataSet ds) {

                //if ds does not contain DataRows
                if (IsValid(ds)) {

                    return ds.Tables[0].Rows[0];
                }
                return null;
            }


            public static DataRow ExtractRowAt(DataSet ds, int index) {

                //if ds does not contain DataRows
                if (IsValid(ds)) {

                    try {
                        return ds.Tables[0].Rows[index];
                    }
                    catch (Exception ex) {
                    }
                }
                return null;
            }


            public static Type GetPropType(object src, string propName) {
                try {

                    Type t = src.GetType();
                    PropertyInfo p = t.GetProperty(propName);
                    Type o = p.PropertyType;
                    return o;
                }
                catch (Exception ex) {
                    return null;
                }
            }


            public static object GetPropValue(object src, string propName)
            {
                try{
                    
                    Type t = src.GetType();
                    PropertyInfo p = t.GetProperty(propName);
                    object o = p.GetValue(src, null);
                    return o;
                }
                catch (Exception ex){
                    return null;
                }
            }



            //Constructs a Class Object from a DataRow
            //Returns that Class Object
            public static T GetItem<T>(DataRow dr) {

                Type customType = typeof(T);
                T obj = Activator.CreateInstance<T>();

                //For each column in the row
                foreach (DataColumn column in dr.Table.Columns) {

                    //look for a matching property name in the customType
                    foreach (PropertyInfo pro in customType.GetProperties()) {

                        //If a match is found, set it
                        if (pro.Name == column.ColumnName)
                            pro.SetValue(obj, dr[column.ColumnName], null);
                        else
                            continue;
                    }
                }

                return obj;
            }


            
            //Populates an object's properties with any mathching columns in a DataRow
            //Returns that object
            public static T SetObjectRowMatches<T>(T toEdit, DataRow dr) {

                Type customType = typeof(T);
                
                //For each column in the row
                foreach (DataColumn column in dr.Table.Columns) {

                    //look for a matching property name in the customType
                    foreach (PropertyInfo prop in customType.GetProperties()) {

                        //If a match is found 
                        if (prop.Name == column.ColumnName) {

                            //Store this property Type & Value
                            Type propType = GetPropType(toEdit, prop.Name);
                            object propValue = GetPropValue(toEdit, prop.Name);

                            //If this type has a generic new() function
                            if (propType.GetMethod("new") != null){
                                
                                //Get GenericMethod
                                var NewTypeMethod = typeof(TypeTools).GetMethod("NewType");

                                //Build Method for this property type
                                var TypedMethod = NewTypeMethod.MakeGenericMethod(new[] { propType });

                                //Invoke Method for this property type
                                var typeDefault = TypedMethod.Invoke(null, null);

                                //If the property is null or default set
                                if (propValue == null || propValue == typeDefault){

                                    //Set this proeprty
                                    prop.SetValue(toEdit, dr[column.ColumnName], null);
                                }
                            }
                            //If the property type has no new default
                            else{

                                //If the property is null
                                if (propValue == null){

                                    //Set this proeprty
                                    prop.SetValue(toEdit, dr[column.ColumnName], null);
                                }
                            }
                        }
                    }
                }

                return toEdit;
            }



            //A version of GetItem that does not require column and property names to match exactly
            //Accepts a 'translation' dictionary that will be used to match column names to property names
            //Dictionary Keys = PropertyName, Values = ColumnName
            private static T GetItem<T>(DataRow dr, Dictionary<string, string> translation, bool autoExactMatch = true) {

                Type customType = typeof(T);
                T obj = Activator.CreateInstance<T>();

                //For each translation 
                foreach (KeyValuePair<string, string> TranslationEntry in translation) {

                    try {
                        //Get property using Translation:Key
                        PropertyInfo prop = customType.GetProperty(TranslationEntry.Key);

                        //Get DB value using Translation:Value
                        object dbValue = dr[TranslationEntry.Value];

                        //Set property to DB value
                        prop.SetValue(obj, dbValue, null);
                    }
                    catch (Exception ex) {}
                }

                //If wanted: match identical column/properties without including them in the translation
                if (autoExactMatch) SetObjectRowMatches<T>(obj, dr);

                return obj;
            }



            //Calls GetItem() on every Row of a DataTable
            private static List<T> ConvertDataTable<T>(DataTable dt, Dictionary<string, string> translation = null, bool autoExactMatch = true) {

                //Construct a List to hold the constructed Objects
                List<T> data = new List<T>();

                //For each Row
                foreach (DataRow row in dt.Rows) {

                    T item;

                    //Construct the Object from the Row
                    if (translation == null) {
                        item = GetItem<T>(row);
                    }
                    else {
                        item = GetItem<T>(row, translation);

                        //If wanted: populate matching fields not in the translation dictionary
                        if (autoExactMatch) {
                            GetItem<T>(row);
                        }
                    }

                    //Store the object in the return List
                    data.Add(item);
                }

                return data;
            }

            

            //Calls ConvertDataTable on a DataSet
            //Returns a list of type T
            //The list contains T objects constructed from the DataRows
            //The Object's property names must exactly match DataBase Column names
            public static List<T> FromDataSet<T>(DataSet data, Dictionary<string, string> translation = null, bool autoMatchExact = true) {

                //If the DataSet has a populated Rows collection
                if (IsValid(data)) {

                    //Build generic List to store return data
                    List<T> myList;

                    //Populate the list with ConvertDataTable()
                    myList = ConvertDataTable<T>(data.Tables[0], translation, autoMatchExact);

                    return myList;
                }

                return null;
            }
        }
    }
}