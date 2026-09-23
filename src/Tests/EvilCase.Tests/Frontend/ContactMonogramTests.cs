using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactMonogramTests
{
    [TestCase("Městský úřad Vzorov, odbor vnitřních věcí", "Mú")]
    [TestCase("Krajský úřad Vzorového kraje", "Kú")]
    [TestCase("Krajský soud ve Vzorově", "KS")]
    [TestCase("Policie Vzorového kraje", "PV")]
    [TestCase("Ministerstvo dopravy", "MD")]
    [TestCase("Ministerstvo vnitra", "MV")]
    [TestCase("Ředitelství silnic a dálnic", "ŘS")]
    [TestCase("Česká advokátní komora", "ČA")]
    [TestCase("starosta Městského úřadu Vzorov", "sM")]
    [TestCase("pověřená úřední osoba", "pú")]
    [TestCase("Vzorek", "V")]
    public void TheMonogramTakesTheFirstLettersOfTheFirstTwoWordsAsTheNameWritesThem(string name, string expected)
    {
        Assert.That(ContactMonogram.Text(name), Is.EqualTo(expected));
    }

    [TestCase("Ing. Petr Vzorek", "PV")]
    [TestCase("Mgr. Jana Vzorková", "JV")]
    public void TheMonogramSkipsALeadingTitle(string name, string expected)
    {
        Assert.That(ContactMonogram.Text(name), Is.EqualTo(expected));
    }
}
