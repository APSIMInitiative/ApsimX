using APSIM.Soils;
using Models;
using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.Core.Tests;

/// <summary>
/// Tests for the DataAccessor class.
/// </summary>
public class DataAccessorTests
{
    class Wrapper<T> : IDataProvider
    {
        public object Data { get; set; }

        public Type Type => typeof(T);
    }

    /// <summary>Checks scalar integer access.</summary>
    [Test]
    public void EnsureConvertToIntegerWorks()
    {
        // string to int
        Assert.That(ApsimConvert.ToType("10", typeof(int)), Is.EqualTo(10));

        // double to int
        Assert.That(ApsimConvert.ToType(10.0, typeof(int)), Is.EqualTo(10));

        // int to int
        Assert.That(ApsimConvert.ToType(10, typeof(int)), Is.EqualTo(10));

        // DateTime to int
        Exception err = Assert.Throws<InvalidCastException>(() => ApsimConvert.ToType(DateTime.Now, typeof(int)));
        Assert.That(err.Message, Is.EqualTo("Invalid cast from 'DateTime' to 'Int32'."));
    }


    /// <summary>Checks scalar double access.</summary>
    [Test]
    public void EnsureToDoubleWorks()
    {
        // string to double
        Assert.That(ApsimConvert.ToType("10", typeof(double)), Is.EqualTo(10.0));

        // double to double
        Assert.That(ApsimConvert.ToType(10.0, typeof(double)), Is.EqualTo(10.0));

        // int to double
        Assert.That(ApsimConvert.ToType(10, typeof(double)), Is.EqualTo(10.0));

        // DateTime to double
        Exception err = Assert.Throws<InvalidCastException>(() => ApsimConvert.ToType(DateTime.Now, typeof(double)));
        Assert.That(err.Message, Is.EqualTo("Invalid cast from 'DateTime' to 'Double'."));
    }


    /// <summary>Checks scalar string access.</summary>
    [Test]
    public void EnsureToStringWorks()
    {
        // string to string
        Assert.That(ApsimConvert.ToType("10.0", typeof(string)), Is.EqualTo("10.0"));

        // double to string
        Assert.That(ApsimConvert.ToType(10.0, typeof(string)), Is.EqualTo("10"));

        // int to string
        Assert.That(ApsimConvert.ToType(10, typeof(string)), Is.EqualTo("10"));

        // DateTime to string
        string dateString = (string)ApsimConvert.ToType(new DateTime(1900, 1, 1), typeof(string));
        DateTime d = DateTime.Parse(dateString);
        Assert.That(d, Is.EqualTo(new DateTime(1900, 1, 1)));
    }

    /// <summary>Ensures scalar to array works.</summary>
    [Test]
    public void EnsureScalarToArrayWorks()
    {
        Assert.That(ApsimConvert.ToType("10,20,30", typeof(int[])), Is.EqualTo(new int[] { 10, 20, 30 }));
        Assert.That(ApsimConvert.ToType("10", typeof(int[])), Is.EqualTo(new int[] { 10 }));
    }

    /// <summary>Ensures scalar to array works.</summary>
    [Test]
    public void EnsureArrayToArrayWorks()
    {
        Assert.That(ApsimConvert.ToType(new double[] { 10, 20, 30 }, typeof(int[])), Is.EqualTo(new int[] { 10, 20, 30 }));
        Assert.That(ApsimConvert.ToType(new string[] { "10", "20", "30" }, typeof(int[])), Is.EqualTo(new int[] { 10, 20, 30 }));
    }

    /// <summary>Ensures array to scalar works.</summary>
    [Test]
    public void EnsureArrayToScalarWorks()
    {
        Assert.That(ApsimConvert.ToType(new double[] { 10 }, typeof(int)), Is.EqualTo(10));

        Exception err = Assert.Throws<Exception>(() => ApsimConvert.ToType(new string[] { "10", "20" }, typeof(int)));
        Assert.That(err.Message, Is.EqualTo("Cannot convert String[] to Int32 because there is more than one value in the array."));
    }

    /// <summary>Ensures scalar to array works.</summary>
    [Test]
    public void EnsureListToListWorks()
    {
        Assert.That(ApsimConvert.ToType(new List<double> { 10, 20, 30 }, typeof(int[])), Is.EqualTo(new List<int> { 10, 20, 30 }));
        Assert.That(ApsimConvert.ToType(new List<string> { "10", "20", "30" }, typeof(int[])), Is.EqualTo(new List<int> { 10, 20, 30 }));
    }

    /// <summary>Ensures array to scalar works.</summary>
    [Test]
    public void EnsureListToScalarWorks()
    {
        Assert.That(ApsimConvert.ToType(new List<double> { 10 }, typeof(int)), Is.EqualTo(10));

        Exception err = Assert.Throws<Exception>(() => ApsimConvert.ToType(new List<string> { "10", "20" }, typeof(int)));
        Assert.That(err.Message, Is.EqualTo("Cannot convert List`1 to Int32 because there is more than one value in the array."));
    }

    /// <summary>Checks array indexing works.</summary>
    [Test]
    public void EnsureDataArrayFilterWithSingleIndexWorks()
    {
        Wrapper<int[]> wrapper = new() { Data = new int[] { 10, 20, 30 } };
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("2")), Is.EqualTo(20));

        // Array index on scalar is not valid.
        Wrapper<int> wrapper2 = new() { Data = 10 };
        Exception err = Assert.Throws<Exception>(() => DataAccessor.Get(wrapper2, new DataArrayFilter("1")));
        Assert.That(err.Message, Is.EqualTo("Array index on a scalar is not valid"));
    }

    /// <summary>Checks array range works.</summary>
    [Test]
    public void EnsureDataArrayFilterWithRangeWorks()
    {
        Wrapper<int[]> wrapper = new() { Data = new int[] { 10, 20, 30 } };
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("2:3")), Is.EqualTo(new int[] { 20, 30 }));
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter(":2")), Is.EqualTo(new int[] { 10, 20 }));
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("2:")), Is.EqualTo(new int[] { 20, 30 }));
    }

    /// <summary>Checks scalar integer access.</summary>
    [Test]
    public void EnsureSetWorks()
    {
        Wrapper<double> wrapper = new() { Data = 0.0 };
        DataAccessor.Set(wrapper, 10);
        Assert.That(wrapper.Data, Is.EqualTo(10));

        DataAccessor.Set(wrapper, "20");
        Assert.That(wrapper.Data, Is.EqualTo(20));

        Wrapper<int[]> arrayWrapper = new() { Data = new int[] { 1, 2 } };
        DataAccessor.Set(arrayWrapper, "20, 30");
        Assert.That(arrayWrapper.Data, Is.EqualTo(new int[] { 20, 30 }));

        Wrapper<int[]> arrayWrapper2 = new() { Data = new int[] { 1, 2 } };
        DataAccessor.Set(arrayWrapper2, "20", new DataArrayFilter("2"));
        Assert.That(arrayWrapper2.Data, Is.EqualTo(new int[] { 1, 20 }));

        Wrapper<int[]> arrayWrapper3 = new() { Data = new int[] { 1, 2 } };
        DataAccessor.Set(arrayWrapper3, "20", new DataArrayFilter("1:"));
        Assert.That(arrayWrapper3.Data, Is.EqualTo(new int[] { 20, 20 }));

        // [Link]s set whole models.
        Wrapper<ISummary> arrayWrapper4 = new() { Data = null };
        Summary summary = new();
        DataAccessor.Set(arrayWrapper4, summary);
        Assert.That(arrayWrapper4.Data, Is.EqualTo(summary));
    }


    /// <summary>Checks that a mm array specifier works.</summary>
    [Test]
    public void EnsureMMArrayFilterWithRangeWorks()
    {
        double[] thickness = [100, 100, 200];
        double[] data = [10, 20, 30];

        Wrapper<double[]> wrapper = new() { Data = data };
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("150mm", thickness)), Is.EqualTo(20.0));
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("200mm", thickness)), Is.EqualTo(20.0));
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("250mm", thickness)), Is.EqualTo(30.0));
    }

    /// <summary>Checks that a mm array specifier works when the depth profile is non integer.</summary>
    [Test]
    public void EnsureMMArrayFilterWithNonIntegerDepthsWorks()
    {
        double[] thickness = [100, 99.999999, 200];
        double[] data = [10, 20, 30];

        Wrapper<double[]> wrapper = new() { Data = data };
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("100mm", thickness)), Is.EqualTo(10.0));
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("200mm", thickness)), Is.EqualTo(20.0));
    }

    /// <summary>
    /// Ensure a MM array specification works with a layer (e.g. soil.water[250mm:350mm]).
    /// </summary>
    [Test]
    public void EnsureArraySpecificationAsMMRangeWorks()
    {
        double[] thickness = [100, 100, 200];
        double[] data = [10, 20, 30];
        //       conc = [0.1, 0.2, 0.15]

        Wrapper<double[]> wrapper = new() { Data = data };
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("250mm:350mm", thickness)), Is.EqualTo(100*0.15));
        Assert.That(DataAccessor.Get(wrapper, new DataArrayFilter("150mm:300mm", thickness)), Is.EqualTo(50*0.2 + 100*0.15));
    }
}
