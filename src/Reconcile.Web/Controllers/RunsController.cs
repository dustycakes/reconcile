using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reconcile.Core.Domain;
using Reconcile.Web.Data;
using Reconcile.Web.Services;

namespace Reconcile.Web.Controllers;

public class RunsController : Controller
{
    private readonly ReconcileDbContext _db;
    private readonly ReconciliationService _service;

    public RunsController(ReconcileDbContext db, ReconciliationService service)
    {
        _db = db;
        _service = service;
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

    public IActionResult New() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile? settlements, IFormFile? donations)
    {
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
        return RedirectToAction(nameof(Details), new { id = run.Id });
    }

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
