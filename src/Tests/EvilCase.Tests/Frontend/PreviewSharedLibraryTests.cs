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
        touchZoom(element, null, () => 1, () => {}, { reset: () => resets++ });
        for (const [x, y, down, toX, toY, up] of JSON.parse(process.argv[2])) {
            listeners.touchstart({ timeStamp: down, touches: [{ clientX: x, clientY: y }] });
            listeners.touchend({ timeStamp: up, touches: [], changedTouches: [{ clientX: toX, clientY: toY }], preventDefault() {} });
        }
        console.log(resets);
        """;

    // Prints the zoom and point apply heard, the content's transform and where the element scrolled to when two fingers
    // on an element scrolled to 100, 100 moved from the first pair of points to the second, while the browser scrolled
    // the element by the offset.
    private const string PanScript = """
        const { touchZoom } = await import(process.argv[1]);
        const listeners = {};
        const element = {
            children: [],
            style: {},
            scrollLeft: 100,
            scrollTop: 100,
            addEventListener: (type, listener) => { listeners[type] = listener; },
            getBoundingClientRect: () => ({ left: 0, top: 0 }),
        };
        const touches = (pair) => pair.map(([x, y]) => ({ clientX: x, clientY: y }));
        const [from, to, [scrollX, scrollY]] = JSON.parse(process.argv[2]);
        let heard = '';
        touchZoom(element, null, () => 1, (zoom, x, y) => { heard = `${zoom} at ${x}, ${y}`; }, { pan: true });
        listeners.touchstart({ touches: touches(from) });
        element.scrollLeft += scrollX;
        element.scrollTop += scrollY;
        listeners.touchmove({ touches: touches(to), preventDefault() {} });
        const transform = element.style.transform;
        listeners.touchend({ touches: [], changedTouches: [], preventDefault() {} });
        console.log(JSON.stringify([transform, heard, `${element.scrollLeft}, ${element.scrollTop}`]));
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

    [Test]
    public async Task APinchKeepsTheContentUnderTheFingersAsTheyMove()
    {
        var (transform, heard, scrolled) = await this.Pan([[100, 100], [200, 100]], [[100, 150], [300, 150]], [0, 0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transform, Is.EqualTo("translate(50px, 50px) scale(2)"), "the content follows the fingers' midpoint during the gesture");
            Assert.That(heard, Is.EqualTo("2 at 150, 100"), "the zoom keeps the point first under the fingers in place");
            Assert.That(scrolled, Is.EqualTo("50, 50"), "the point then scrolls to where the fingers lifted");
        }
    }

    [Test]
    public async Task AScrollTheBrowserMakesDuringAPinchMovesTheContentOnlyOnce()
    {
        var (transform, heard, scrolled) = await this.Pan([[100, 100], [200, 100]], [[100, 150], [300, 150]], [-50, 0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transform, Is.EqualTo("translate(0px, 50px) scale(2)"));
            Assert.That(heard, Is.EqualTo("2 at 200, 100"), "the point first under the fingers moved with the scroll");
            Assert.That(scrolled, Is.EqualTo("50, 50"), "the content ends where the fingers lifted, whatever the browser scrolled");
        }
    }

    private async Task<(string Transform, string Heard, string Scrolled)> Pan(int[][] from, int[][] to, int[] scroll)
    {
        var output = await this.library.Run(PanScript, JsonSerializer.Serialize<object[]>([from, to, scroll]));
        var printed = JsonSerializer.Deserialize<string[]>(output)!;

        return (printed[0], printed[1], printed[2]);
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
