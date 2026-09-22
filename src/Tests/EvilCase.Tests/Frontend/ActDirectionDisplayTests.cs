using System.Diagnostics;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Acts;
using TabBlazor;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActDirectionDisplayTests
{
    [Test]
    public void EveryDirectionReadsInCzechAndCarriesAColour()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var direction in Enum.GetValues<ActDirection>())
            {
                Assert.That(ActDirectionDisplay.Text(direction), Is.Not.Empty, $"{direction}: every known act direction renders text");
                Assert.That(ActDirectionDisplay.Color(direction), Is.Not.EqualTo(TablerColor.Default), $"{direction}: every known act direction carries its own colour");
            }
        }
    }

    [Test]
    public void AnActWithNoDirectionShowsADash()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActDirectionDisplay.Text(direction: null), Is.EqualTo("—"), "an act without a direction is still a row on the screen");
            Assert.That(ActDirectionDisplay.Color(direction: null), Is.EqualTo(TablerColor.Default));
        }
    }

    [Test]
    public void ADirectionTheAppDoesNotKnowIsNeverDisplayed()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(static () => ActDirectionDisplay.Text((ActDirection)99), Throws.InstanceOf<UnreachableException>(), "a direction the app does not name never renders as a dash");
            Assert.That(static () => ActDirectionDisplay.Color((ActDirection)99), Throws.InstanceOf<UnreachableException>());
        }
    }
}
