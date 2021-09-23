using AutoComplete.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace AutoComplete.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private DisposalModel DisposalModel { get; init; }

        private SortedDictionary<DisposalModel.LabelKey, List<DisposalModel.Address>> AddressDictionary { get; init; }

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

        public IActionResult LookupAddress(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                string area = DisposalModel.GetAreaFromAddress(address);
                string areaName = DisposalModel.GetAreaName(area);
                List<DisposalModel.AreaSchedule> greyScheduleList = DisposalModel.GetGreyScheduleListFromArea(area);
                List<DisposalModel.AreaSchedule> blueScheduleList = DisposalModel.GetBlueScheduleListFromArea(area);
                ViewBag.Address = address;
                return View(new DisposalInformation(address, area, areaName, greyScheduleList, blueScheduleList));
            }
            return View();
        }

        public DisposalInformation LookupAddressREST(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                string area = DisposalModel.GetAreaFromAddress(address);
                string areaName = DisposalModel.GetAreaName(area);
                List<DisposalModel.AreaSchedule> greyScheduleList = DisposalModel.GetGreyScheduleListFromArea(area);
                List<DisposalModel.AreaSchedule> blueScheduleList = DisposalModel.GetBlueScheduleListFromArea(area);
                return new DisposalInformation(address, area, areaName, greyScheduleList, blueScheduleList);
            }
            return null;
        }

        public record DisposalInformation(
            string Address,
            string Area,
            string AreaName,
            List<DisposalModel.AreaSchedule> GreyScheduleList,
            List<DisposalModel.AreaSchedule> BlueScheduleList
        );

        public List<DisposalModel.LabelValue> AutoCompleteSearch(string term, bool suppressExact = true)
        {
            return DisposalModel.AutoCompleteSearch(term, suppressExact);
        }

        public string AutoCompleteSearchP(string term, string callback, bool suppressExact = true)
        {
            string serialized = JsonConvert.SerializeObject(DisposalModel.AutoCompleteSearch(term, suppressExact));
            return $"{callback}({serialized})";
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
