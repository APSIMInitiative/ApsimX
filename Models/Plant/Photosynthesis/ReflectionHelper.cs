using Models.PMF.Photosynthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PSAppServerCoreNet.Classes
{
    /// <summary></summary>
    public class ReflectionHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static ModelPar getAttributeByName(string modelName, string propertyName)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);
            return getModelAttribute(p);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static ModelPar getModelAttribute(PropertyInfo p)
        {
            ModelPar par = null;
            par = (ModelPar)p.GetCustomAttribute(typeof(ModelPar));
            return par;
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static object mapInternalModel(string modelName, PhotosynthesisModel ps)
        {
            object model = null;

            if (modelName == "LeafCanopy")
            {
                model = ps.canopy;
            }
            else if (modelName == "PathwayParameters")
            {
                model = ps.canopy.CPath;
            }
            else if (modelName == "EnvironmentModel")
            {
                model = ps.envModel;
            }
            else if (modelName == "PhotosynthesisModel")
            {
                model = ps;
            }

            return model;
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <param name="ps"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static object getValueByNameLayer(string modelName, string propertyName, PhotosynthesisModel ps, int layer)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            object model = mapInternalModel(modelName, ps);

            double[] values = null;

            values = (double[])p.GetValue(model);

            return values[layer];
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <param name="ps"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static object getCurveDataByNameLayer(string modelName, string propertyName, PhotosynthesisModel ps, int layer)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            return p.GetValue(ps);
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <param name="ps"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static object[] getLayeredSSValueByNameLayer(string modelName, string propertyName, PhotosynthesisModel ps, int layer)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            double[] values = null;
            object[] result = new object[2];

            values = (double[])p.GetValue(ps.sunlit);
            result[0] = values[0];

            values = (double[])p.GetValue(ps.shaded);
            result[1] = values[0];

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static object getValueByName(string modelName, string propertyName, PhotosynthesisModel ps)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            object model = mapInternalModel(modelName, ps);

            return p.GetValue(model);
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <param name="ps"></param>
        /// <param name="value"></param>
        public static void setValueByName(string modelName, string propertyName, PhotosynthesisModel ps, object value)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            object model = mapInternalModel(modelName, ps);

            p.SetValue(model, value);
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static PropertyInfo getPropertyByName(string modelName, string propertyName)
        {
            //Assembly ass = (Assembly)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name.Contains("Models.PMF.Photosynthesis"));

            //Type model = ass.GetTypes().First(t => t.Name == modelName);

            Type model = Type.GetType("Models.PMF.Photosynthesis." + modelName);

            List<PropertyInfo> props = new List<PropertyInfo>(model.GetProperties().Where(p => p.Name == propertyName));

            if (props.Count == 0)
            {
                return null;
            }

            return props[0];
        }
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="model"></param>
        /// <param name="prop"></param>
        public static void getPropertyByGUID(string guid, out Type model, out PropertyInfo prop)
        {
            model = null;
            prop = null;


            //Assembly ass = (Assembly)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name.Contains("Models.PMF.Photosynthesis"));
            //Type ass = Type()
            //List<Type> types = new List<Type>(ass.GetTypes());
            List<Type> types = new List<Type>();

            List<string> modelNames = new List<string>(new string[] { "LeafCanopy", "PathwayParameters", "EnvironmentModel", "PhotosynthesisModel" });


            //Iterate through the types
            foreach (Type t in types)
            {
                //Look for the UID and find the model and the property
                List<PropertyInfo> props = new List<PropertyInfo>(t.GetProperties()
                    .Where(p => p.GetCustomAttribute(typeof(ModelPar)) != null &&
                     ((ModelPar)p.GetCustomAttribute(typeof(ModelPar))).id == guid));

                if (props.Count > 0)
                {
                    prop = props[0];
                    model = t;
                    return;
                }
            }
        }
    }
}
