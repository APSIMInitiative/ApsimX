// -----------------------------------------------------------------------
// <copyright file="ReportTable.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    public partial class ReportTable
    {
        /// <summary>An enumerator for rows of the table</summary>
        private class RowEnumerator : IEnumerator<object[]>
        {
            private FileStream dataFile;
            private Column[] columns;
            private Section[] sections;
            private int currentSectionIndex = -1;
            private int rowIndex = -1;
            private int[] valueIndexes;
            private object[] values;

            /// <summary>Constructor</summary>
            /// <param name="dataFileName">The file where the row data is stored.</param>
            /// <param name="columns">Columns definitions for table</param>
            public RowEnumerator(string dataFileName, Column[] columns)
            {
                dataFile = File.Open(dataFileName, FileMode.Open, FileAccess.Read);
                this.columns = columns;
            }

            /// <summary>Constructor</summary>
            /// <param name="sections">The file where the row data is stored.</param>
            /// <param name="columns">Columns definitions for table</param>
            public RowEnumerator(Section[] sections, ReportTable.Column[] columns)
            {
                this.sections = sections;
                this.columns = columns;
            }

            /// <summary>Return the current value</summary>
            public object Current
            {
                get
                {
                    return values;
                }
            }

            /// <summary>Return the current value</summary>
            object[] IEnumerator<object[]>.Current
            {
                get
                {
                    return Current as object[];
                }
            }

            /// <summary>Dispose of object</summary>
            public void Dispose()
            {
                if (dataFile != null)
                    dataFile.Close();
            }

            /// <summary>Return the current value</summary>
            public bool MoveNext()
            {
                rowIndex++;
                if (rowIndex >= NumRowsInSection())
                {
                    if (!MoveToNextSection())
                    {
                        values = null;
                        return false;
                    }
                }

                for (int i = 0; i < columns.Length; i++)
                {
                    int valueIndex = valueIndexes[i];
                    if (valueIndex != -1)
                    {
                        if (sections[currentSectionIndex].Columns[valueIndex] is ReportColumnConstantValue)
                            values[i] = sections[currentSectionIndex].Columns[valueIndex].Values[0];
                        else if (rowIndex < sections[currentSectionIndex].Columns[valueIndex].Values.Count)
                            values[i] = sections[currentSectionIndex].Columns[valueIndex].Values[rowIndex];
                    }
                    else
                        values[i] = null;
                }

                return true;
            }

            /// <summary>Reset the enumerator</summary>
            public void Reset()
            {
                if (dataFile != null)
                    dataFile.Seek(0, SeekOrigin.Begin);
                rowIndex = -1;
                currentSectionIndex = -1;
                values = null;
            }

            private bool MoveToNextSection()
            {
                if (dataFile != null)
                {
                    IFormatter formatter = new BinaryFormatter();
                    Array.Resize(ref sections, 1);
                    sections[0] = formatter.Deserialize(dataFile) as ReportTable.Section;
                    rowIndex = 0;
                    currentSectionIndex = 0;
                    IndexBlock();
                    return true;
                }
                else
                {
                    currentSectionIndex++;
                    rowIndex = 0;
                    IndexBlock();
                    return currentSectionIndex < sections.Length;
                }
            }

            /// <summary>Calculate and return the number of rows in current section.</summary>
            private int NumRowsInSection()
            {
                if (currentSectionIndex < 0 || currentSectionIndex >= sections.Length ||
                    sections[currentSectionIndex].Columns.Count == 0)
                    return 0;

                int numRows = 0;
                for (int i = 0; i < sections[currentSectionIndex].Columns.Count; i++)
                    numRows = Math.Max(numRows, sections[currentSectionIndex].Columns[i].Values.Count);
                return numRows;
            }

            /// <summary>Index the current section</summary>
            private void IndexBlock()
            {
                // Create an array of value indexes for column.
                Array.Resize(ref valueIndexes, columns.Length);
                Array.Resize(ref values, columns.Length);
                for (int i = 0; i < columns.Length; i++)
                    valueIndexes[i] = sections[currentSectionIndex].Columns.FindIndex(c => c.Name == columns[i].Name);
            }
        }
    }
}