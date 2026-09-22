using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Cases;
using TabBlazor;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseStatusDisplayTests
{
    [Test]
    public void EveryStatusReadsInCzechAndCarriesAColour()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var status in Enum.GetValues<CaseStatus>())
            {
                Assert.That(CaseStatusDisplay.Text(status), Is.Not.Empty, $"{status}: every known case status renders text");
                Assert.That(CaseStatusDisplay.Color(status), Is.Not.EqualTo(TablerColor.Default), $"{status}: every known case status carries its own colour");
            }
        }
    }

    [Test]
    public void EveryFilterReadsInCzech()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var filter in Enum.GetValues<CaseStatusFilter>())
                Assert.That(CaseStatusDisplay.FilterText(filter), Is.Not.Empty, $"{filter}: every known case status filter renders text");
        }
    }

    [Test]
    public void AStatusTheAppDoesNotKnowIsNeverDisplayed()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(static () => CaseStatusDisplay.Text((CaseStatus)99), Throws.InstanceOf<ArgumentOutOfRangeException>(), "a status the app does not name never renders as an empty label");
            Assert.That(static () => CaseStatusDisplay.Color((CaseStatus)99), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(static () => CaseStatusDisplay.FilterText((CaseStatusFilter)99), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
