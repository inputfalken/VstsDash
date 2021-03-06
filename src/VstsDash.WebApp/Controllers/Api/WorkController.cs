using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VstsDash.AppServices.WorkActivity;
using VstsDash.AppServices.WorkIteration;
using VstsDash.RestApi;

namespace VstsDash.WebApp.Controllers.Api
{
    [Route("api/work", Name = RouteNames.ApiWork)]
    public class WorkController : ControllerBase
    {
        private readonly IIterationsApiService _iterationsApi;

        private readonly ITeamsApiService _teamsApi;

        private readonly WorkActivityAppService _workActivityAppService;

        private readonly WorkIterationAppService _workIterationAppService;

        public WorkController(
            AppSettings appSettings,
            IIterationsApiService iterationsApi,
            IWorkApiService workApi,
            ITeamsApiService teamsApi,
            WorkActivityAppService workActivityAppService,
            WorkIterationAppService workIterationAppService)
            : base(appSettings, workApi)
        {
            if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));
            if (workApi == null) throw new ArgumentNullException(nameof(workApi));

            _iterationsApi = iterationsApi ?? throw new ArgumentNullException(nameof(iterationsApi));
            _teamsApi = teamsApi ?? throw new ArgumentNullException(nameof(teamsApi));
            _workActivityAppService = workActivityAppService ??
                                      throw new ArgumentNullException(nameof(workActivityAppService));
            _workIterationAppService = workIterationAppService ??
                                       throw new ArgumentNullException(nameof(workIterationAppService));
        }

        [HttpGet("teamdoneefforts")]
        public async Task<IActionResult> TeamDoneEfforts(
            string projectId = null,
            string teamId = null,
            string iterationId = null)
        {
            var idParams = await GetEnsuredIdParams(projectId, teamId, iterationId);

            var efforts = await _workIterationAppService.GetWorkIterationDoneEffortsPerDay(
                idParams.ProjectId,
                idParams.TeamId,
                idParams.IterationId);

            var jsonData = efforts.Select(x => new object[] {x.Key, x.Value}).ToList();

            return Json(jsonData);
        }

        [HttpGet("teamactivities")]
        public async Task<IActionResult> TeamActivities(
            string projectId = null,
            string teamId = null,
            string iterationId = null)
        {
            var jsonData = await GetWorkActivityJsonData(projectId, teamId, iterationId);

            return Json(jsonData);
        }

        [HttpGet("memberactivities/{memberId}")]
        public async Task<IActionResult> MemberActivities(
            Guid memberId,
            string projectId = null,
            string teamId = null,
            string iterationId = null)
        {
            var jsonData = await GetWorkActivityJsonData(projectId, teamId, iterationId, memberId);

            return Json(jsonData);
        }

        private async Task<IList<object[]>> GetWorkActivityJsonData(
            string projectId,
            string teamId,
            string iterationId,
            Guid? memberId = null)
        {
            var idParams = await GetEnsuredIdParams(projectId, teamId, iterationId);

            var activity = await _workActivityAppService.GetActivity(
                idParams.ProjectId,
                idParams.TeamId,
                idParams.IterationId);

            var iterationTask = _iterationsApi.Get(idParams.ProjectId, idParams.TeamId, idParams.IterationId);
            var teamDaysOffTask =
                _iterationsApi.GetTeamDaysOff(idParams.ProjectId, idParams.TeamId, idParams.IterationId);

            await Task.WhenAll(iterationTask, teamDaysOffTask);

            var iteration = iterationTask.Result;
            var teamDaysOff = teamDaysOffTask.Result;

            var teamCapacity = new TeamCapacity(iteration, teamDaysOff);

            var commits = activity.Commits;

            if (memberId != null)
            {
                commits = commits.Where(x => memberId.Value == x.Author.MemberId).ToList();

                var capacitiesTask =
                    _iterationsApi.GetCapacities(idParams.ProjectId, idParams.TeamId, idParams.IterationId);
                var teamMembersTask = _teamsApi.GetMembers(idParams.ProjectId, idParams.TeamId);

                await Task.WhenAll(capacitiesTask, teamMembersTask);

                teamCapacity = new TeamCapacity(iteration, teamDaysOff, teamMembersTask.Result, capacitiesTask.Result);
            }

            var memberCapacity = teamCapacity.Members.FirstOrDefault(x => x.MemberId == memberId);

            var workDays = memberCapacity?.WorkDays ?? teamCapacity.WorkDays;
            var hasAnyMemberWorkDays = memberCapacity?.WorkDays?.Any() ?? false;

            var dates = teamCapacity.IterationWorkDays;

            return (from date in dates
                    let dayCommits = commits
                        .Where(x => (x.Commit.AuthorDate ?? DateTimeOffset.MinValue).Date == date.Date)
                        .ToList()
                    let hasCommits = dayCommits.Any()
                    let isWorkDay = workDays.Contains(date)
                    let isPastWorkDay = isWorkDay && date.Date <= DateTime.UtcNow.Date
                    let shouldIncludeData = hasCommits || !hasAnyMemberWorkDays || isPastWorkDay
                    let commitCount = shouldIncludeData ? dayCommits.Count : (int?) null
                    let totalChangeCount = shouldIncludeData
                        ? dayCommits.Sum(c => c.Commit.TotalChangeCount)
                        : (int?) null
                    select new object[] {date, commitCount, totalChangeCount})
                .ToList();
        }
    }
}