using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Reconcile.Core.Domain;
using Reconcile.Web.Data;
using Reconcile.Web.Services;

namespace Reconcile.Web.Controllers;

public class RunsController : Controller
{
    private readonly ReconcileDbContext _db;
    private readonly ReconciliationService _service;
    private readonly DemoOptions _demo;

    public RunsController(ReconcileDbContext db, ReconciliationService service, IOptions<DemoOptions> demo)
    {
        _db = db;
        _service = service;
        _demo = demo.Value;
    }

    public async Task<IActionResult> Index()
    {
        var runs = await _db.Runs
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new RunSummary(
                r.Id, r.CreatedAtUtc, r.SettlementFileName, r.DonationFileName,
                r.SettlementLines.Count, r.DonationRecords.Count,
                r.MatchResults.Count(m => m.Status != MatchStatus.Matched && m.Status != MatchStatus.ProbableMatch)))
            .ToListAsync();
        return View(runs);
    }

    public IActionResult New()
    {
        ViewData["DemoMode"] = _demo.DemoMode;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile? settlements, IFormFile? donations)
    {
        if (_demo.DemoMode)
        {
            // The public demo accepts no uploads: nothing a visitor sends is stored.
            return StatusCode(StatusCodes.Status403Forbidden, "This demo runs sample data only. Clone the repository to reconcile your own files.");
        }
        if (settlements is null || donations is null)
        {
            ModelState.AddModelError("", "Both files are required: the processor settlement export and the CRM donation export.");
            return View(nameof(New));
        }

        using var s = new StreamReader(settlements.OpenReadStream());
        using var d = new StreamReader(donations.OpenReadStream());
        var run = await _service.RunAsync(settlements.FileName, s, donations.FileName, d);
        return RedirectToAction(nameof(Details), new { id = run.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSample()
    {
        var run = await _service.RunSampleAsync();
        if (_demo.DemoMode)
        {
            // Every visitor's click adds a run; keep the list short. Deleting a run
            // cascades to its lines, records and results.
            var keep = await _db.Runs.OrderByDescending(r => r.Id).Take(_demo.KeepRuns).Select(r => r.Id).ToListAsync();
            await _db.Runs.Where(r => !keep.Contains(r.Id)).ExecuteDeleteAsync();
        }
        return RedirectToAction(nameof(Details), new { id = run.Id });
    }

    public IActionResult Error() => View();

    public async Task<IActionResult> Details(int id)
    {
        var run = await LoadRun(id);
        return run is null ? NotFound() : View(run);
    }

    public async Task<IActionResult> Report(int id)
    {
        var run = await LoadRun(id);
        return run is null ? NotFound() : View(run);
    }

    private async Task<ReconciliationRun?> LoadRun(int id) =>
        await _db.Runs
            .Include(r => r.MatchResults).ThenInclude(m => m.SettlementLine)
            .Include(r => r.MatchResults).ThenInclude(m => m.DonationRecord)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id);

    public record RunSummary(
        int Id, DateTime CreatedAtUtc, string SettlementFile, string DonationFile,
        int SettlementCount, int DonationCount, int FindingCount);
}
