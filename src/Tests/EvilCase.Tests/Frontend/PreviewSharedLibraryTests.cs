using System.Text.Json;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class PreviewSharedLibraryTests
{
    // Prints the zoom a pinch from the start zoom leads to when the fingers moved from the first distance to the second.
    private const string Script = """
        const { pinchZoom } = await import(process.argv[1]);
        const [start, from, to] = JSON.parse(process.argv[2]);
        console.log(JSON.stringify(pinchZoom(start, from, to)));
        """;

    // Prints how many times one-finger touches, each [x, y, down time, lift x, lift y, lift time], reset the zoom.
    private const string TapScript = """
        const { touchZoom } = await import(process.argv[1]);
        const listeners = {};
        const element = { addEventListener: (type, listener) => { listeners[type] = listener; } };
        let resets = 0;
        touchZoom(element, null, () => 1, () => {}, () => resets++);
        for (const [x, y, down, toX, toY, up] of JSON.parse(process.argv[2])) {
            listeners.touchstart({ timeStamp: down, touches: [{ clientX: x, clientY: y }] });
            listeners.touchend({ timeStamp: up, touches: [], changedTouches: [{ clientX: toX, clientY: toY }], preventDefault() {} });
        }
        console.log(resets);
        """;

    private ServedLibrary library = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.library = new ServedLibrary("/js/preview-shared.js");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.library.Dispose();
    }

    [Test]
    public async Task APinchScalesTheZoomByTheFingersSpread()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.Pinch(1, 100, 150), Is.EqualTo(1.5).Within(0.0001));
            Assert.That(await this.Pinch(2, 200, 100), Is.EqualTo(1).Within(0.0001));
        }
    }

    [Test]
    public async Task APinchStopsAtTheZoomSteps()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.Pinch(2, 100, 400), Is.EqualTo(4), "the largest zoom step bounds a pinch out");
            Assert.That(await this.Pinch(1, 400, 100), Is.EqualTo(0.5), "the smallest zoom step bounds a pinch in");
        }
    }

    [Test]
    public async Task AFitOutsideTheStepsBoundsAPinchOfItsOwn()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.Pinch(0.3, 100, 50), Is.EqualTo(0.3).Within(0.0001), "a pinch in never makes a fit below the steps larger");
            Assert.That(await this.Pinch(0.3, 100, 200), Is.EqualTo(0.6).Within(0.0001));
        }
    }

    [Test]
    public async Task OnlyTwoQuickTapsResetTheZoom()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.Touches([100, 100, 0, 102, 101, 80], [100, 100, 200, 100, 100, 260]), Is.EqualTo(1));
            Assert.That(await this.Touches([100, 300, 0, 100, 100, 80], [100, 300, 200, 100, 100, 260]), Is.Zero, "two flicks are a scroll, not a double tap");
            Assert.That(await this.Touches([100, 100, 0, 100, 100, 80], [100, 100, 500, 100, 100, 560]), Is.Zero, "two slow taps are no double tap");
        }
    }

    private async Task<int> Touches(params int[][] touches)
    {
        var output = await this.library.Run(TapScript, JsonSerializer.Serialize(touches));

        return JsonSerializer.Deserialize<int>(output);
    }

    private async Task<double> Pinch(double start, double from, double to)
    {
        var output = await this.library.Run(Script, JsonSerializer.Serialize<double[]>([start, from, to]));

        return JsonSerializer.Deserialize<double>(output);
    }
}
