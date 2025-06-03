using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class HomeModel : PageModel
{
    private readonly AppDbContext _db;

    public HomeModel(AppDbContext db)
    {
        _db = db;
    }
}