namespace Models.Core.ApsimFile
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    class Writer : XmlTextWriter
    {
        public Writer(TextWriter w) 
            : base(w)
        {
            version = Converter.LatestVersion;
        }
       

        Stack<string> elements = new Stack<string>();

        enum States { InModelNode, InModel, NameProperty, WritingProperty, InModelType, LookingForChildren }

        States currentState = States.InModelNode;

        string foundName = null;
        int version = -1;

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
           
            switch (currentState)
            {
                case States.InModelNode:
                    if (localName == "Name")
                        currentState = States.NameProperty;
                    else if (localName == "Model")
                        currentState = States.InModel;
                    else if (localName == "Child")
                        currentState = States.InModelNode;
                    break;
                default:
                    base.WriteStartElement(prefix, localName, ns);  // regular model element.
                    currentState = States.WritingProperty;
                    break;
            }

//                base.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            switch (currentState)
            {
                case States.InModel:
                    currentState = States.InModelNode;
                    break;
                case States.WritingProperty:
                    base.WriteEndElement();
                    currentState = States.InModel;
                    break;
                case States.NameProperty:
                    currentState = States.InModelNode;
                    break;
                case States.InModelNode:
                    base.WriteEndElement();
                    break;
            }
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            if (localName == "Version")
                base.WriteStartAttribute(prefix, localName, ns);
            else
            {
                switch (currentState)
                {
                    case States.InModel:
                    case States.LookingForChildren:
                        if (localName == "type")
                            currentState = States.InModelType;
                        break;
                }
            }
        }

        public override void WriteRaw(string data)
        {
            base.WriteRaw(data);
        }

        public override void WriteValue(int value)
        {
            base.WriteValue(value);
        }

        public override void WriteEndAttribute()
        {
            //switch (currentState)
            //{
            //    case States.InModel:
            //        base.WriteEndAttribute();
            //        break;
            //}
        }
    


        public override void WriteNmToken(string name)
        {
            base.WriteNmToken(name);
        }


        public override void WriteString(string text)
        {
            switch (currentState)
            {
                case States.InModel:
                case States.WritingProperty:
                    base.WriteString(text);
                    break;
                case States.NameProperty:
                    foundName = text;
                    break;
                case States.InModelType:
                    // text will be the model type.
                    base.WriteStartElement(text);

                    // Write version element.
                    if (version != -1)
                    {
                        base.WriteStartAttribute("Version");
                        base.WriteString(version.ToString());
                        base.WriteEndAttribute();

                       // base.WriteAttributeString("Version", version.ToString());
                        version = -1;
                    }

                    // Write name element.
                    if (foundName != null)
                    {
                      //  base.WriteStartElement("Name");
                      //  base.WriteString(foundName);
                      //  base.WriteEndElement();
                        foundName = null;

                    }
                    currentState = States.InModel;
                    break;
            }
        }

        public override void WriteValue(string value)
        {

        }

        public void DoWriteStartElement(string elementName)
        {
            //_skip = false;
            //WriteStartElement(elementName);
            base.WriteStartElement(null, elementName, null);
            //_skip = true;
        }


        public void DoWriteEndElement()
        {
            //_skip = false;
            base.WriteEndElement();
            //_skip = true;
        }


        //public override void WriteStartAttribute(string prefix, string localName, string ns)
        //{
        //    // STEP 1 - Omits XSD and XSI declarations.
        //    // From Kzu - http://weblogs.asp.net/cazzu/archive/2004/01/23/62141.aspx
        //    if (prefix == "xmlns" && (localName == "xsd" || localName == "xsi"))
        //    {
        //        _skip = true;
        //        return;
        //    }
        //    base.WriteStartAttribute(prefix, localName, ns);
        //}

        ////public override void WriteString(string text)
        ////{
        ////    if (_skip) return;
        ////    base.WriteString(text);
        ////}

        //public override void WriteEndAttribute()
        //{
        //    if (_skip)
        //    {
        //        // Reset the flag, so we keep writing.
        //        _skip = false;
        //        return;
        //    }
        //    base.WriteEndAttribute();
        //}

        //public override void WriteStartDocument()
        //{
        //    // STEP 2: Do nothing so we omit the xml declaration.
        //}
    }
}
