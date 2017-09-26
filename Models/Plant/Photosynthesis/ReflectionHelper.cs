using Models.PMF.Phenology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PSAppServerCoreNet.Classes
{
    public class ReflectionHelper
    {
        public static ModelPar getAttributeByName(string modelName, string propertyName)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);
            return getModelAttribute(p);
        }
        //----------------------------------------------------------------------------------------------------------------
        public static ModelPar getModelAttribute(PropertyInfo p)
        {
            ModelPar par = null;
            par = (ModelPar)p.GetCustomAttribute(typeof(ModelPar));
            return par;
        }
        //----------------------------------------------------------------------------------------------------------------
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
        public static object getValueByNameLayer(string modelName, string propertyName, PhotosynthesisModel ps, int layer)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            object model = mapInternalModel(modelName, ps);

            double[] values = null;

            values = (double[])p.GetValue(model);

            return values[layer];
        }
        //----------------------------------------------------------------------------------------------------------------
        public static object getCurveDataByNameLayer(string modelName, string propertyName, PhotosynthesisModel ps, int layer)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            return p.GetValue(ps);
        }
        //----------------------------------------------------------------------------------------------------------------
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
        public static object getValueByName(string modelName, string propertyName, PhotosynthesisModel ps)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            object model = mapInternalModel(modelName, ps);

            return p.GetValue(model);
        }
        //----------------------------------------------------------------------------------------------------------------
        public static void setValueByName(string modelName, string propertyName, PhotosynthesisModel ps, object value)
        {
            PropertyInfo p = getPropertyByName(modelName, propertyName);

            object model = mapInternalModel(modelName, ps);

            p.SetValue(model, value);
        }
        //----------------------------------------------------------------------------------------------------------------
        public static PropertyInfo getPropertyByName(string modelName, string propertyName)
        {
            //Assembly ass = (Assembly)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name.Contains("Models.PMF.Phenology"));

            //Type model = ass.GetTypes().First(t => t.Name == modelName);

            Type model = Type.GetType("Models.PMF.Phenology." + modelName);

            List<PropertyInfo> props = new List<PropertyInfo>(model.GetProperties().Where(p => p.Name == propertyName));

            if (props.Count == 0)
            {
                return null;
            }

            return props[0];
        }
        //----------------------------------------------------------------------------------------------------------------
        public static void getPropertyByGUID(string guid, out Type model, out PropertyInfo prop)
        {
            model = null;
            prop = null;


            //Assembly ass = (Assembly)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name.Contains("Models.PMF.Phenology"));
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

        ////----------------------------------------------------------------------------------------------------------------
        //public static PropertyInfo getPropertyByGUID(string guid)
        //{
        //    Assembly ass = (Assembly)AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name.Contains("Models.PMF.Phenology"));

        //    List<PropertyInfo> allProps = new List<PropertyInfo>(ass.GetType().GetProperties());

        //    List<PropertyInfo> props = new List<PropertyInfo>(allProps
        //        .Where(p => p.GetCustomAttribute(typeof(ModelPar)) != null &&
        //         ((ModelPar)p.GetCustomAttribute(typeof(ModelPar))).id == guid));

        //    if (props.Count == 0)
        //    {
        //        return null;
        //    }

        //    return props[0];
        //}
    }
}
