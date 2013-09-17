using System;
using System.Collections.Generic;

namespace Models.Core
{
    public abstract class ModelCollection : Model
    {
        /// <summary>
        /// Return a full path to this system. Does not include the 'Simulation' node.
        /// Format: .PaddockName.ChildName
        /// </summary>
        public override string FullPath
        {
            get
            {
                if (this is Simulation)
                    return ".";
                else if (Parent is Simulation)
                    return "." + Name;
                else
                    return Parent.FullPath + "." + Name;
            }
        }

        /// <summary>
        /// A list of child models.
        /// </summary>
        public abstract List<Model> Models { get; set; }

        /// <summary>
        /// Add a model to the Models collection
        /// </summary>
        public abstract void AddModel(Model Model);

        /// <summary>
        /// Remove a model from the Models collection
        /// </summary>
        public abstract bool RemoveModel(Model Model);

        /// <summary>
        /// Return a model of the specified type that is in scope. Returns null if none found.
        /// </summary>
        public override Model Find(Type ModelType)
        {
            if (ModelType.IsAssignableFrom(GetType()))
                return this;
            foreach (Model Child in Models)
            {
                if (ModelType.IsAssignableFrom(Child.GetType()))
                    return Child;
            }

            // If we get this far then search the simulation
            if (Parent == null)
                return null;
            else
                return Parent.Find(ModelType);
        }

        /// <summary>
        /// Return a model with the specified name is in scope. Returns null if none found.
        /// </summary>
        public override Model Find(string ModelName)
        {
            if (Name == ModelName)
                return this;
            foreach (Model Child in Models)
            {
                if (Child.Name == ModelName)
                    return Child;
            }

            // If we get this far then search the simulation
            if (Parent == null)
                return null;
            else
                return Parent.Find(ModelName);
        }

        /// <summary>
        /// Return a model or variable using the specified NamePath. Returns null if not found.
        /// </summary>
        public override object Get(string NamePath)
        {
            object Obj = base.Get(NamePath);
            if (Obj != null)
                return Obj;

            Obj = this;

            string[] NamePathBits = NamePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string PathBit in NamePathBits)
            {
                object LocalObj = null;
                if (Obj is ModelCollection)
                    LocalObj = (Obj as ModelCollection).FindChild(PathBit);

                if (LocalObj != null)
                    Obj = LocalObj;
                else
                {
                    object Value = Utility.Reflection.GetValueOfFieldOrProperty(PathBit, Obj);
                    if (Value == null)
                        return null;
                    else
                        Obj = Value;
                }
            }
            return Obj;
        }

        /// <summary>
        /// Find a child model with the specified name. Returns null if not found.
        /// </summary>
        protected Model FindChild(string NameToFind)
        {
            foreach (Model Child in Models)
            {
                if (Child.Name == NameToFind)
                    return Child;
            }
            return null;
        }

    }
}
