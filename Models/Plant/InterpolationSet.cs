using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Plant
{
   /*
       Private Class InterpSet


        Public Shared Widening Operator CType(ByVal d As InterpSet) As String
            Return ""
        End Operator
        Public Shared Narrowing Operator CType(ByVal Xml As String) As InterpSet
            Dim Table As New Xml.XmlDocument()
            Table.LoadXml(Xml)
            Dim NumPoints As Integer = XmlHelper.ChildNodes(Table.DocumentElement, "").Count
            Dim Result As New InterpSet
            ReDim Result.XVals(NumPoints - 1)
            ReDim Result.YVals(NumPoints - 1)
            Dim i As Integer = -1
            For Each point As Xml.XmlNode In XmlHelper.ChildNodes(Table.DocumentElement, "")
                i = i + 1
                Result.XVals(i) = Convert.ToSingle(XmlHelper.Value(point, "x"))
                Result.YVals(i) = Convert.ToSingle(XmlHelper.Value(point, "y"))
            Next
            Return Result
        End Operator
    End Class

   class InterpolationSet
      {
      double XVals();
      double YVals();

      public Single value(Single x)
         {
         Boolean flag;
         return Utility.Math.LinearInterpReal(CType(x, Single), XVals, YVals, flag);
         }

      public string shared widening operator CType (InterpolationSet d )
         {
         return "";
         }
   
      public InterpolcationSet Shared Narrowing Operator CType(String Xml)
            Dim Table As New Xml.XmlDocument()
            Table.LoadXml(Xml)
            Dim NumPoints As Integer = XmlHelper.ChildNodes(Table.DocumentElement, "").Count
            Dim Result As New InterpSet
            ReDim Result.XVals(NumPoints - 1)
            ReDim Result.YVals(NumPoints - 1)
            Dim i As Integer = -1
            For Each point As Xml.XmlNode In XmlHelper.ChildNodes(Table.DocumentElement, "")
                i = i + 1
                Result.XVals(i) = Convert.ToSingle(XmlHelper.Value(point, "x"))
                Result.YVals(i) = Convert.ToSingle(XmlHelper.Value(point, "y"))
            Next
            Return Result
        End Operator

*/

      
   }
