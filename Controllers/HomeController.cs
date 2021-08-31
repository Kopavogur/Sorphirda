using AutoComplete.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace AutoComplete.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private DisposalModel DisposalModel { get; init; }

        private Dictionary<DisposalModel.LabelKey, List<DisposalModel.Address>> AddressDictionary { get; init; }

        public HomeController(
            DisposalModel disposalModel,
            ILogger<HomeController> logger
        )
        {
            _logger = logger;
            DisposalModel = disposalModel;
            AddressDictionary = DisposalModel.AddressDictionary;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Form(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                string area = DisposalModel.GetAreaFromAddress(address);
                string areaName = DisposalModel.GetAreaName(area);
                List<DisposalModel.AreaSchedule> scheduleList = DisposalModel.GetScheduleListFromArea(area);
                ViewBag.Address = address;
                return View(new DisposalInformation(area, areaName, scheduleList));
            }
            return View();
        }

        public record DisposalInformation(
            string Area,
            string AreaName,
            List<DisposalModel.AreaSchedule> ScheduleList
        );

        public JsonResult AutoCompleteSearch(string term)
        {
            return Json(DisposalModel.AutoCompleteSearch(term));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
