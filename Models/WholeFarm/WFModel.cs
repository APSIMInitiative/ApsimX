using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm
{
	///<summary>
	/// WholeFarm base model
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public abstract class WFModel: Model
	{
		[Link]
		ISummary Summary = null;

		private string id = "00001";

		/// <summary>
		/// Model identifier
		/// </summary>
		public string ID { get { return id; } }

		/// <summary>
		/// Method to set defaults from   
		/// </summary>
		public void SetDefaults()
		{
			//Iterate through properties
			foreach (var property in GetType().GetProperties())
			{
				//Iterate through attributes of this property
				foreach (Attribute attr in property.GetCustomAttributes(true))
				{
					//does this property have [DefaultValueAttribute]?
					if (attr is System.ComponentModel.DefaultValueAttribute)
					{
						//So lets try to load default value to the property
						System.ComponentModel.DefaultValueAttribute dv = (System.ComponentModel.DefaultValueAttribute)attr;
						try
						{
							//Is it an array?
							if (property.PropertyType.IsArray)
							{
								//Use set value for arrays
								property.SetValue(this, null, (object[])dv.Value);
							}
							else
							{
								//Use set value for.. not arrays
								property.SetValue(this, dv.Value, null);
							}
						}
						catch (Exception ex)
						{
							Summary.WriteWarning(this, ex.Message);
							//eat it... Or maybe Debug.Writeline(ex);
						}
					}
				}
			}
		}


	}
}
