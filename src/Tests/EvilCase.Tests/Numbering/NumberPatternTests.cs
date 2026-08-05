using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberPatternTests
{
    private static readonly DateOnly Date = new(2026, 8, 4);

    [Test]
    public void TheDefaultPatternsWriteTheNumbersTheVisionShows()
    {
        var caseNumber = NumberPattern.Format(NumberingDefaults.CaseNumberPattern, Date, 1);
        var actNumber = NumberPattern.Format(NumberingDefaults.ActNumberPattern, new DateOnly(2026, 8, 5), 2, caseNumber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseNumber, Is.EqualTo("EC-20260804-001"));
            Assert.That(actNumber, Is.EqualTo("EC-20260804-001-20260805-002"), "an act number is written under the case's own number");
        }
    }

    /// <summary>
    /// The width is a minimum, so a series past it keeps counting. Sorting as text holds up to the
    /// width and no further, which is what the operator is choosing when they widen it.
    /// </summary>
    [Test]
    public void TheSequenceIsPaddedToTheWidthThePatternNamesAndKeepsCountingPastIt()
    {
        string[] narrow = [NumberPattern.Format("{seq}", Date, 1), NumberPattern.Format("{seq}", Date, 999), NumberPattern.Format("{seq}", Date, 1000)];
        string[] wide = [NumberPattern.Format("{seq:6}", Date, 1), NumberPattern.Format("{seq:6}", Date, 999999), NumberPattern.Format("{seq:6}", Date, 1000000)];

        string[] expectedNarrow = ["001", "999", "1000"];
        string[] expectedWide = ["000001", "999999", "1000000"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(narrow, Is.EqualTo(expectedNarrow), "a {seq} naming no width is three digits");
            Assert.That(wide, Is.EqualTo(expectedWide), "{seq:6} is six, and the millionth number grows rather than being capped or cut");
            Assert.That(narrow.Take(2), Is.Ordered.Using<string>(StringComparer.Ordinal), "the padding is what makes a series sort as text as far as its width goes");
            Assert.That(wide.Take(2), Is.Ordered.Using<string>(StringComparer.Ordinal));
        }
    }

    [Test]
    public void AWidthThatIsNotAPositiveNumberOfDigitsIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{seq:6}", NumberPatternKind.CaseNumber), Is.Null);
            Assert.That(NumberPattern.Validate("EC-{seq:0}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.SequenceWidth), "no digits at all is not a width");
            Assert.That(NumberPattern.Validate("EC-{seq:-2}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.SequenceWidth));
            Assert.That(NumberPattern.Validate("EC-{seq:six}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.SequenceWidth));
            Assert.That(NumberPattern.Validate("EC-{seq:}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.SequenceWidth));
            Assert.That(NumberPattern.Validate("EC-{seq:600}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.TooLongForItsColumn));
            Assert.That(NumberPattern.Validate("EC-{seq:2000000000}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.TooLongForItsColumn), "a width no column could hold is measured against the column, never written out to find out");
        }
    }

    /// <summary>
    /// Two of them and the digits of one run into the digits of the other: <c>{seq}{seq}</c> writes
    /// <c>12341234</c> for 1234, which reads back as 12341 and 234 just as well.
    /// </summary>
    [Test]
    public void MoreThanOneSequenceIsRefusedBecauseNothingCanReadItBack()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{seq}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatedSequence));
            Assert.That(NumberPattern.Validate("EC-{seq}-{seq:6}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatedSequence), "two widths do not tell them apart either");
            Assert.That(NumberPattern.Validate("EC-{seq}", NumberPatternKind.CaseNumber), Is.Null);
        }
    }

    /// <summary>
    /// The second shape nothing can read back. A case number is read as anything at all — an act keeps
    /// the number its case was called by — so what says where it ends is the text in front of the
    /// <c>{seq}</c>, and a date part writes digits like the sequence does: <c>{case-number}{seq}</c>
    /// writes <c>EC-20261000</c> for case <c>EC-2026</c>, which is case <c>EC-2</c> at 0261000 just as
    /// well.
    /// </summary>
    [Test]
    public void ACaseNumberWithNothingButDigitsBetweenItAndTheSequenceIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("{case-number}{seq}", NumberPatternKind.ActNumber), Is.EqualTo(NumberPatternError.CaseNumberBesideTheSequence));
            Assert.That(NumberPattern.Validate("A{case-number}{seq}/x", NumberPatternKind.ActNumber), Is.EqualTo(NumberPatternError.CaseNumberBesideTheSequence), "text at the edges of the pattern says nothing about where the two meet");
            Assert.That(NumberPattern.Validate("{seq}{case-number}", NumberPatternKind.ActNumber), Is.EqualTo(NumberPatternError.CaseNumberBesideTheSequence), "and a sequence in front of it runs into a case number opening with digits");
            Assert.That(
                NumberPattern.Validate("{case-number}{year}{month}{day}{seq}", NumberPatternKind.ActNumber),
                Is.EqualTo(NumberPatternError.CaseNumberBesideTheSequence),
                "a date part is digits by the time the number is read back, so it separates nothing");
            Assert.That(NumberPattern.Validate("{case-number}-{seq}", NumberPatternKind.ActNumber), Is.Null, "one character that is not a digit is what fixes the end of the case number");
            Assert.That(NumberPattern.Validate("{seq}/{case-number}", NumberPatternKind.ActNumber), Is.Null);
            Assert.That(NumberPattern.Validate("{case-number}{year}-{seq}", NumberPatternKind.ActNumber), Is.Null, "and it can stand anywhere between the two");
        }
    }

    [Test]
    public void WhatThePatternDoesNotNameIsWrittenOutAsItStands()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Format("EC/{unknown}/{seq}", Date, 7), Is.EqualTo("EC/{unknown}/007"), "formatting writes an unknown placeholder out; refusing it is validation's job");
            Assert.That(NumberPattern.Format("{case-number}/{seq}", Date, 7), Is.EqualTo("/007"), "a case pattern naming the case number has none to write");
        }
    }

    [Test]
    public void TheDefaultsAreUsable()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate(NumberingDefaults.CaseNumberPattern, NumberPatternKind.CaseNumber), Is.Null);
            Assert.That(NumberPattern.Validate(NumberingDefaults.ActNumberPattern, NumberPatternKind.ActNumber), Is.Null);
        }
    }

    [Test]
    public void APatternThePlaceholdersDoNotCoverIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{quarter}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.UnknownPlaceholder));
            Assert.That(NumberPattern.Validate("EC-{year}-{seq", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.UnknownPlaceholder), "a brace with no partner is as unusable as an unknown name");
            Assert.That(NumberPattern.Validate("EC-{year}{month}{day}/{case-number}-{seq}", NumberPatternKind.ActNumber), Is.Null, "all five names are known");
        }
    }

    [Test]
    public void OnlyAnActPatternMayNameTheCaseNumber()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                NumberPattern.Validate("{case-number}EC-{seq}", NumberPatternKind.CaseNumber),
                Is.EqualTo(NumberPatternError.CaseNumberOutsideAnActPattern),
                "a placeholder in the wrong field is not one the application does not know, and the screen says something else about each");
            Assert.That(NumberPattern.Format("{case-number}EC-{seq}", Date, 1), Is.EqualTo("EC-001"), "what the refusal is about");
            Assert.That(NumberPattern.Validate("{case-number}EC-{seq}", NumberPatternKind.ActNumber), Is.Null);
        }
    }

    [Test]
    public void APatternWithoutTheSequenceIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{year}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.NoSequence), "without {seq} every case gets the same number");
            Assert.That(NumberPattern.Validate("EC-{year}-{seq}", NumberPatternKind.CaseNumber), Is.Null);
        }
    }

    /// <summary>
    /// The widest a pattern ever writes is what has to fit: a series counted to <c>int.MaxValue</c> is
    /// ten digits — or the width, where that is wider — and <c>{case-number}</c> can be a case number
    /// filling its own column.
    /// </summary>
    [Test]
    public void APatternThatWouldNotFitTheColumnItIsStoredInIsRefused()
    {
        var caseBudget = Case.CaseNumberLength - "2147483647".Length;
        var actBudget = Act.ActNumberLength - "2147483647".Length - Case.CaseNumberLength - "-".Length;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate(new string('X', caseBudget) + "{seq}", NumberPatternKind.CaseNumber), Is.Null);
            Assert.That(
                NumberPattern.Validate(new string('X', caseBudget + 1) + "{seq}", NumberPatternKind.CaseNumber),
                Is.EqualTo(NumberPatternError.TooLongForItsColumn),
                "a pattern that overflows its column fails on the insert");
            Assert.That(NumberPattern.Validate(new string('X', actBudget) + "{case-number}-{seq}", NumberPatternKind.ActNumber), Is.Null);
            Assert.That(
                NumberPattern.Validate(new string('X', actBudget + 1) + "{case-number}-{seq}", NumberPatternKind.ActNumber),
                Is.EqualTo(NumberPatternError.TooLongForItsColumn),
                "an act number carries a whole case number, so it runs out of its own column that much sooner");
            Assert.That(
                NumberPattern.Validate(new string('X', caseBudget) + "{seq:20}", NumberPatternKind.CaseNumber),
                Is.EqualTo(NumberPatternError.TooLongForItsColumn),
                "a width wider than the ten digits an int reaches is what the pattern is measured by");
        }
    }

    /// <summary>
    /// A case number is the operator's to type, so it can hold pattern text of its own; an act number
    /// writes it out as it stands rather than reading it as a pattern.
    /// </summary>
    [Test]
    public void AHandTypedCaseNumberIsWrittenOutRatherThanFormattedAgain()
    {
        var written = NumberPattern.Format("{case-number}-{year}{month}{day}-{seq}", new DateOnly(2026, 8, 5), 1, "SP-{day}/2026");

        Assert.That(written, Is.EqualTo("SP-{day}/2026-20260805-001"), "the case number goes in last, so no later placeholder pass runs over it");
    }

    /// <summary>
    /// What <see cref="NumberPatternError.TooLongForItsColumn"/> measures is what the pattern writes,
    /// for a case number of the widest kind there is: one made of placeholders, filling its own column.
    /// </summary>
    [Test]
    public void TheColumnBoundHoldsForACaseNumberMadeOfPlaceholders()
    {
        const string pattern = "{case-number}-{seq}";
        var caseNumber = string.Concat(Enumerable.Repeat("{seq}", 12)) + new string('X', Case.CaseNumberLength - (12 * "{seq}".Length));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseNumber, Has.Length.EqualTo(Case.CaseNumberLength));
            Assert.That(NumberPattern.Validate(pattern, NumberPatternKind.ActNumber), Is.Null);
            Assert.That(
                NumberPattern.Format(pattern, DateOnly.MaxValue, int.MaxValue, caseNumber),
                Has.Length.LessThanOrEqualTo(Act.ActNumberLength),
                "a pattern the validation let through fits its column whatever the case number says");
        }
    }

    /// <summary>
    /// The series counts within the finest part the pattern names, so a pattern that writes that part
    /// without the coarser ones repeats itself: <c>EC-{day}-{seq}</c> issued <c>EC-05-001</c> on the
    /// fifth of August and again on the fifth of September.
    /// </summary>
    [Test]
    public void APatternThatWouldWriteOneNumberTwiceIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{day}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatingPeriod));
            Assert.That(NumberPattern.Validate("EC-{month}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatingPeriod));
            Assert.That(NumberPattern.Validate("EC-{month}{day}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatingPeriod), "the year is missing whichever finer part is named");
            Assert.That(NumberPattern.Validate("EC-{year}{month}-{seq}", NumberPatternKind.CaseNumber), Is.Null, "a month written under its year counts monthly and stays distinct");
            Assert.That(NumberPattern.Validate("EC-{seq}", NumberPatternKind.CaseNumber), Is.Null, "one series forever writes each number once");
        }
    }
}
