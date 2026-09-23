using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactMonogramTests
{
    [TestCase("Městský úřad Vzorov, odbor vnitřních věcí", "MÚ")]
    [TestCase("Vzorek", "V")]
    [TestCase("pověřená úřední osoba", "PÚ")]
    [TestCase("Česká advokátní komora", "ČA")]
    public void TheMonogramTakesTheFirstLettersOfTheFirstTwoWords(string name, string expected)
    {
        Assert.That(ContactMonogram.Text(name), Is.EqualTo(expected));
    }
}
