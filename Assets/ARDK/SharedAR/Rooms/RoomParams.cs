// Copyright 2022 Niantic, Inc. All Rights Reserved.
namespace Niantic.Experimental.ARDK.SharedAR
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public enum RoomVisibility : byte
  {
    Unknown = 0,
    // Publicly visible and can be found through the ExperienceService
    Public,
    // Private room that can only be joined through RoomId
    Private
  }

  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public struct RoomParams
  {
    public string RoomID {get; internal set;}
    public RoomVisibility Visibility { get; internal set; }
    
    public int Capacity { get; internal set; }

    public string Name { get; internal set; }

    public string ExperienceId { get; internal set; }

    public string Description { get; internal set; }

    public string Passcode { internal get; set; }

    public RoomParams
    (      
      int capacity,
      string name = "",
      string experienceId = "",
      string description = "",
      string passcode = "",
      RoomVisibility visibility = RoomVisibility.Public
    ) : this("", capacity, name, experienceId, description, passcode, visibility: visibility)
    {
    }

    internal RoomParams
    (
      string id,
      RoomVisibility visibility = RoomVisibility.Public
    ) : this(id, default, visibility: visibility)
    {
    }
    
    internal RoomParams
    (
      string id,
      int capacity,
      string name = "",
      string experienceId = "",
      string description = "",
      string passcode = "",
      RoomVisibility visibility = RoomVisibility.Public
    )
    {
      RoomID = id;
      Capacity = capacity;
      Name = name;
      ExperienceId = experienceId;
      Description = description;
      Visibility = visibility;
      // Don't apply a passcode unless the room is private
      Passcode = Visibility == RoomVisibility.Private ? passcode : "";
    }
  }
} // namespace Niantic.ARDK.SharedAR