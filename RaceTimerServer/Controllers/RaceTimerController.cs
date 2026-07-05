using Microsoft.AspNetCore.Mvc;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;
using RaceTimerServer.Controllers.Requests;

namespace RaceTimerServer.Controllers;

/// <summary>
/// REST API for race management, participants, and timing data.
/// Provides full CRUD operations for races, participants, and timing points.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RaceTimerController : ControllerBase
{
    private readonly IRaceRepository _repo;
    private readonly ILogger<RaceTimerController> _logger;

    public RaceTimerController(IRaceRepository repo, ILogger<RaceTimerController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    #region Races

    /// <summary>
    /// Gets all races
    /// </summary>
    [HttpGet("races")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Race>>> GetRaces()
    {
        var races = await _repo.GetAllRacesAsync();
        return Ok(races);
    }

    /// <summary>
    /// Gets a specific race by ID
    /// </summary>
    [HttpGet("races/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Race>> GetRace(Guid id)
    {
        var race = await _repo.GetRaceAsync(id);
        if (race == null) return NotFound();
        return Ok(race);
    }

    /// <summary>
    /// Gets races by status
    /// </summary>
    [HttpGet("races/status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Race>>> GetRacesByStatus(string status)
    {
        if (!Enum.TryParse<RaceStatus>(status, ignoreCase: true, out var raceStatus))
            return BadRequest($"Invalid status: {status}");

        var races = await _repo.GetRacesByStatusAsync(raceStatus);
        return Ok(races);
    }

    /// <summary>
    /// Creates a new race
    /// </summary>
    [HttpPost("races")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Race>> CreateRace([FromBody] CreateRaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Race name cannot be empty");

        var created = await _repo.AddRaceAsync(request.Name);
        if (created == null) return Conflict("Failed to create race");
        return CreatedAtAction(nameof(GetRace), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing race
    /// </summary>
    [HttpPut("races/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRace(Guid id, [FromBody] Race race)
    {
        if (id != race.Id) return BadRequest("ID mismatch");
        await _repo.UpdateRaceAsync(race);
        return NoContent();
    }

    /// <summary>
    /// Deletes a race
    /// </summary>
    [HttpDelete("races/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRace(Guid id)
    {
        var ok = await _repo.DeleteRaceAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Starts a race with the given participants
    /// </summary>
    [HttpPost("races/{id}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> StartRace(Guid id, [FromBody] StartRaceRequest request)
    {
        if (request.ParticipantIds == null || !request.ParticipantIds.Any())
            return BadRequest("At least one participant is required");

        var result = await _repo.StartRaceAsync(id, request.TimePointUtc ?? DateTime.UtcNow, [..request.ParticipantIds]);
        return Ok(result);
    }

    /// <summary>
    /// Finishes a race
    /// </summary>
    [HttpPost("races/{id}/finish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> FinishRace(Guid id)
    {
        await _repo.CheckForRaceCompletionAsync(id);
        return Ok(true);
    }

    #endregion

    #region Participants

    /// <summary>
    /// Gets all participants
    /// </summary>
    [HttpGet("participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Participant>>> GetParticipants()
    {
        var participants = await _repo.GetAllParticipantsAsync();
        return Ok(participants);
    }

    /// <summary>
    /// Gets a specific participant by ID
    /// </summary>
    [HttpGet("participants/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Participant>> GetParticipant(Guid id)
    {
        var participant = await _repo.GetParticipantAsync(id);
        if (participant == null) return NotFound();
        return Ok(participant);
    }

    /// <summary>
    /// Creates a new participant
    /// </summary>
    [HttpPost("participants")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<Participant>> CreateParticipant([FromBody] CreateParticipantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Participant name cannot be empty");

        var participant = await _repo.CreateParticipantAsync(request.Name);
        if (participant == null) return Conflict("Failed to create participant");
        return CreatedAtAction(nameof(GetParticipant), new { id = participant.Id }, participant);
    }

    /// <summary>
    /// Updates a participant
    /// </summary>
    [HttpPut("participants/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateParticipant(Guid id, [FromBody] Participant participant)
    {
        if (id != participant.Id) return BadRequest("ID mismatch");
        await _repo.UpdateParticipantAsync(participant);
        return NoContent();
    }

    /// <summary>
    /// Deletes a participant
    /// </summary>
    [HttpDelete("participants/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParticipant(Guid id)
    {
        await _repo.DeleteParticipantAsync(id);
        return NoContent();
    }

    #endregion

    #region Race Participants

    /// <summary>
    /// Gets all participants for a specific race
    /// </summary>
    [HttpGet("races/{raceId}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RaceParticipant>>> GetRaceParticipants(Guid raceId)
    {
        var participants = await _repo.GetRacesParticipantsAsync(raceId);
        return Ok(participants);
    }

    /// <summary>
    /// Assigns a participant to a race
    /// </summary>
    [HttpPost("races/{raceId}/participants/{participantId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceParticipant>> AssignParticipantToRace(Guid raceId, Guid participantId)
    {
        var raceParticipant = await _repo.AssignParticipantToRaceAsync(raceId, participantId);
        return CreatedAtAction(nameof(GetRaceParticipants), new { raceId }, raceParticipant);
    }

    /// <summary>
    /// Removes a participant from a race
    /// </summary>
    [HttpDelete("races/{raceId}/participants/{participantId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParticipantFromRace(Guid raceId, Guid participantId)
    {
        var ok = await _repo.RemoveParticipantFromRaceAsync(raceId, participantId);
        if (!ok) return NotFound();
        return NoContent();
    }

    #endregion

    #region Race Time Points

    /// <summary>
    /// Gets all time points for a race
    /// </summary>
    [HttpGet("races/{raceId}/timepoints")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RaceTimePoint>>> GetRaceTimePoints(Guid raceId)
    {
        var timePoints = await _repo.GetRaceTimePointsAsync(raceId);
        return Ok(timePoints);
    }

    /// <summary>
    /// Creates a new time point for a race
    /// </summary>
    [HttpPost("races/{raceId}/timepoint")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceTimePoint>> CreateRaceTimePoint(Guid raceId, [FromBody] CreateTimePointRequest request)
    {
        var timePoint = await _repo.CreateRaceTimePointAsync(raceId, request.Name);
        if (timePoint == null) return NotFound();
        return CreatedAtAction(nameof(GetRaceTimePoints), new { raceId }, timePoint);
    }

    /// <summary>
    /// Updates time points for a race
    /// </summary>
    [HttpPut("races/{raceId}/timepoints")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRaceTimePoints(Guid raceId, [FromBody] List<RaceTimePoint> timePoints)
    {
        var ok = await _repo.UpdateTimePointsAsync(raceId, timePoints);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Deletes a time point
    /// </summary>
    [HttpDelete("races/{raceId}/timepoints/{timePointId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRaceTimePoint(Guid raceId, Guid timePointId)
    {
        var ok = await _repo.DeleteRaceTimePointAsync(timePointId);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Copies time points from one race to another
    /// </summary>
    [HttpPost("races/{raceIdCopyFrom}/timepoints/copy-to/{raceIdCopyTo}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> CopyTimePoints(Guid raceIdCopyFrom, Guid raceIdCopyTo)
    {
        var ok = await _repo.CopyRaceTimePointsAsync(raceIdCopyFrom, raceIdCopyTo);
        if (!ok) return NotFound();
        return Ok(true);
    }

    #endregion

    #region Race Participant Time Points

    /// <summary>
    /// Gets all participant time points for a race
    /// </summary>
    [HttpGet("timepoints/race/{raceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RaceParticipantTimePoint>>> GetRaceParticipantTimePoints(Guid raceId)
    {
        var timePoints = await _repo.GetRaceParticipantTimePointsForRaceAsync(raceId);
        return Ok(timePoints);
    }

    /// <summary>
    /// Gets a specific participant time point
    /// </summary>
    [HttpGet("timepoints/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceParticipantTimePoint>> GetRaceParticipantTimePoint(Guid id)
    {
        var timePoint = await _repo.GetRaceParticipantTimePointAsync(id);
        if (timePoint == null) return NotFound();
        return Ok(timePoint);
    }

    /// <summary>
    /// Gets all unassigned time points
    /// </summary>
    [HttpGet("timepoints/unassigned")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RaceParticipantTimePoint>>> GetUnassignedTimePoints()
    {
        var timePoints = await _repo.GetUnassignedTimepointsAsync();
        return Ok(timePoints ?? []);
    }

    /// <summary>
    /// Creates a new unassigned time point
    /// </summary>
    [HttpPost("timepoints")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<RaceParticipantTimePoint>> CreateUnassignedTimePoint([FromBody] CreateUnassignedTimePointRequest request)
    {
        var timePoint = await _repo.AddUnassignedTimePointAsync(request.TimePointUtc);
        return CreatedAtAction(nameof(GetRaceParticipantTimePoint), new { id = timePoint.Id }, timePoint);
    }

    /// <summary>
    /// Assigns a time point to a participant in a race
    /// </summary>
    [HttpPost("timepoints/{timePointId}/assign/{participantId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> AssignTimePointToParticipant(Guid timePointId, Guid participantId, [FromQuery] Guid? raceId = null)
    {
        if (!raceId.HasValue)
            return BadRequest("raceId query parameter is required");

        var ok = await _repo.AssignTimePointToRaceParticipantAsync(timePointId, raceId.Value, participantId);
        return Ok(ok);
    }

    /// <summary>
    /// Deletes a participant time point
    /// </summary>
    [HttpDelete("timepoints/{timePointId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRaceParticipantTimePoint(Guid timePointId)
    {
        var ok = await _repo.DeleteRaceParticipantTimePointAsync(timePointId);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Sets a penalty time for a time point
    /// </summary>
    [HttpPost("timepoints/{timePointId}/penalty")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> SetTimePointPenalty(Guid timePointId, [FromBody] TimeSpan penaltyTime)
    {
        var ok = await _repo.SetRaceParticipantTimePointPenaltyTime(timePointId, penaltyTime);
        if (!ok) return NotFound();
        return Ok(true);
    }

    /// <summary>
    /// Corrects a time point
    /// </summary>
    [HttpPost("timepoints/{timePointId}/correct")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> CorrectTimePoint(Guid timePointId, [FromBody] CorrectTimePointRequest request)
    {
        var ok = await _repo.CorrectTimePointAsync(timePointId, request.CorrectedTimeUtc, request.Reason, request.CorrectedByUser);
        if (!ok) return NotFound();
        return Ok(true);
    }

    /// <summary>
    /// Undoes a time point correction
    /// </summary>
    [HttpPost("timepoints/{timePointId}/undo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> UndoTimePointCorrection(Guid timePointId)
    {
        var ok = await _repo.UndoTimePointCorrectionAsync(timePointId);
        if (!ok) return NotFound();
        return Ok(true);
    }

    #endregion

    #region Change Tracking (Differential Queries)

    /// <summary>
    /// Gets changes since a specific timestamp for a race
    /// </summary>
    [HttpGet("races/{raceId}/changes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetChangesSince(Guid raceId, [FromQuery] DateTime sinceUtc)
    {
        var changes = await _repo.GetChangesSinceAsync(raceId, sinceUtc);
        return Ok(changes);
    }

    #endregion
}
