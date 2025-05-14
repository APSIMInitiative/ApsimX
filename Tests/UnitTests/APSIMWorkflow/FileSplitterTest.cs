using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using APSIM.Shared.Utilities;
using APSIM.Workflow;
using Models.Core;
using Models.PostSimulationTools;
using Models.Storage;
using Moq;
using NUnit.Framework;
using ClosedXML.Excel;
using Models;
using Models.Factorial;

namespace UnitTests.APSIMWorkflowTests;


[TestFixture]
public class FileSplitterTest
{

    private string oldDirectory;
    private string newDirectory;
    private Simulations sims;

    [SetUp]
    public void SetUp()
    {
        oldDirectory = Path.Combine(Path.GetTempPath(), "OldDirectory/");
        newDirectory = Path.Combine(Path.GetTempPath(), "NewDirectory/");
        sims = new Simulations
        {
            Children =
            [
                new Experiment
                {
                    Children = [
                        new Factors
                        {
                            Children = [
                                new Factor
                                {
                                    Name = "Factor1",
                                    Specification = "Factor1=0 to 10 step 2"
                                },
                            ]
                        },
                        new Simulation
                        {
                            Name = "ExampleSim",
                        },
                    ]
                },
                new DataStore
                {
                    Children =
                    [
                        new ExcelInput
                        {
                            FileNames = ["file1.xlsx", "file2.xlsx"],
                            SheetNames = ["Sheet1", "Sheet2"]
                        }
                    ]
                },
                new Folder()
            ]
        };

        // Directory setup
        Directory.CreateDirectory(oldDirectory);
        Directory.CreateDirectory(newDirectory);

    }

    [Test]
    public void CopyObservedData_ValidInput_CopiesDataCorrectly()
    {
        // Arrange
        // Excel setup
        XLWorkbook workbook = new XLWorkbook();
        var sheet1 = workbook.Worksheets.Add("Sheet1");
        sheet1.Cell(1, 1).Value = "SimulationName";
        sheet1.Cell(1, 2).Value = "Clock.Today";
        sheet1.Cell(2, 1).Value = "experimentfactor10";
        sheet1.Cell(2, 2).Value = "2025-01-01";
        
        var sheet2 = workbook.Worksheets.Add("Sheet2");
        sheet2.Cell(1, 1).Value = "SimulationName";
        sheet2.Cell(1, 2).Value = "Clock.Today";
        sheet2.Cell(2, 1).Value = "experimentfactor10";
        sheet2.Cell(2, 2).Value = "2025-01-01";
        workbook.SaveAs(oldDirectory + "/file1.xlsx");

        XLWorkbook workbook2 = new XLWorkbook();
        var secondSheet1 = workbook2.Worksheets.Add("Sheet1");
        secondSheet1.Cell(1, 1).Value = "SimulationName";
        secondSheet1.Cell(1, 2).Value = "Clock.Today";
        secondSheet1.Cell(2, 1).Value = "experimentfactor10";
        secondSheet1.Cell(2, 2).Value = "2025-01-01";
        var secondSheet2 = workbook2.Worksheets.Add("Sheet2");
        secondSheet2.Cell(1, 1).Value = "SimulationName";
        secondSheet2.Cell(1, 2).Value = "Clock.Today";
        secondSheet2.Cell(2, 1).Value = "experimentfactor10";
        secondSheet2.Cell(2, 2).Value = "2025-01-01";
        workbook2.SaveAs(oldDirectory + "/file2.xlsx");

        Folder simsFolder = sims.FindChild<Folder>();

        // Act
        FileSplitter.CopyObservedData(sims, simsFolder, oldDirectory, newDirectory);

        // Assert
        Assert.That(File.Exists(newDirectory + "file1.xlsx"), Is.True);
        Assert.That(File.Exists(newDirectory + "file2.xlsx"), Is.True);

    }

    [Test]
    public void CopyObservedData_NoData_DoesNotCopyFiles()
    {
        // Arrange
        // Excel setup
        XLWorkbook workbook = new XLWorkbook();
        var sheet1 = workbook.Worksheets.Add("Sheet1");
        workbook.SaveAs(oldDirectory + "file1.xlsx");

        XLWorkbook workbook2 = new XLWorkbook();
        var secondSheet1 = workbook2.Worksheets.Add("Sheet1");
        workbook2.SaveAs(oldDirectory + "file2.xlsx");

        Folder simsFolder = sims.FindChild<Folder>();

        // Act
        FileSplitter.CopyObservedData(sims, simsFolder, oldDirectory, newDirectory);

        // Assert
        Assert.That(File.Exists(newDirectory + "file1.xlsx"), Is.False);
        Assert.That(File.Exists(newDirectory + "file2.xlsx"), Is.False);

    }

    [TearDown]
    public void TearDown()
    {
        // Helper to delete directory if it exists
        void DeleteDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                    // Sometimes files are still locked, try again
                    System.Threading.Thread.Sleep(100);
                    Directory.Delete(dir, true);
                }
                catch { /* Ignore further exceptions for cleanup */ }
            }
        }

        DeleteDirectory(oldDirectory);
        DeleteDirectory(newDirectory);
    }
}
