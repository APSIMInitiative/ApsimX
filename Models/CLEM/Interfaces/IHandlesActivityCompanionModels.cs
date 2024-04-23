namespace Models.CLEM.Interfaces
{
    internal interface IHandlesActivityCompanionModels
    {
        /// <summary>
        /// A method to get a list of activity specified labels for a generic type T 
        /// </summary>
        /// <param name="type">The type of model to return labels for</param>
        /// <returns>A LabelsForCompanionModels containing all labels</returns>
        public LabelsForCompanionModels DefineCompanionModelLabels(string type);
    }
}
