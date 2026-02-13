import 'package:json_annotation/json_annotation.dart';

part 'directions_response.g.dart';

@JsonSerializable()
class DirectionsResponse {
  final List<DirectionRoute> routes;
  final String status;
  @JsonKey(name: 'error_message')
  final String? errorMessage;

  DirectionsResponse({
    required this.routes,
    required this.status,
    this.errorMessage,
  });

  factory DirectionsResponse.fromJson(Map<String, dynamic> json) =>
      _$DirectionsResponseFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionsResponseToJson(this);
}

@JsonSerializable()
class DirectionRoute {
  final DirectionBounds bounds;
  final String copyrights;
  final List<DirectionLeg> legs;
  @JsonKey(name: 'overview_polyline')
  final DirectionPolyline overviewPolyline;
  final String summary;
  final List<dynamic> warnings;
  @JsonKey(name: 'waypoint_order')
  final List<dynamic> waypointOrder;

  DirectionRoute({
    required this.bounds,
    required this.copyrights,
    required this.legs,
    required this.overviewPolyline,
    required this.summary,
    required this.warnings,
    required this.waypointOrder,
  });

  factory DirectionRoute.fromJson(Map<String, dynamic> json) =>
      _$DirectionRouteFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionRouteToJson(this);
}

@JsonSerializable()
class DirectionBounds {
  final DirectionLatLng northeast;
  final DirectionLatLng southwest;

  DirectionBounds({required this.northeast, required this.southwest});

  factory DirectionBounds.fromJson(Map<String, dynamic> json) =>
      _$DirectionBoundsFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionBoundsToJson(this);
}

@JsonSerializable()
class DirectionLatLng {
  final double lat;
  final double lng;

  DirectionLatLng({required this.lat, required this.lng});

  factory DirectionLatLng.fromJson(Map<String, dynamic> json) =>
      _$DirectionLatLngFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionLatLngToJson(this);
}

@JsonSerializable()
class DirectionLeg {
  final DirectionDistance distance;
  final DirectionDuration duration;
  @JsonKey(name: 'end_address')
  final String endAddress;
  @JsonKey(name: 'end_location')
  final DirectionLatLng endLocation;
  @JsonKey(name: 'start_address')
  final String startAddress;
  @JsonKey(name: 'start_location')
  final DirectionLatLng startLocation;
  final List<DirectionStep> steps;
  @JsonKey(name: 'traffic_speed_entry')
  final List<dynamic>? trafficSpeedEntry;
  @JsonKey(name: 'via_waypoint')
  final List<dynamic>? viaWaypoint;

  DirectionLeg({
    required this.distance,
    required this.duration,
    required this.endAddress,
    required this.endLocation,
    required this.startAddress,
    required this.startLocation,
    required this.steps,
    this.trafficSpeedEntry,
    this.viaWaypoint,
  });

  factory DirectionLeg.fromJson(Map<String, dynamic> json) =>
      _$DirectionLegFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionLegToJson(this);
}

@JsonSerializable()
class DirectionDistance {
  final String text;
  final int value;

  DirectionDistance({required this.text, required this.value});

  factory DirectionDistance.fromJson(Map<String, dynamic> json) =>
      _$DirectionDistanceFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionDistanceToJson(this);
}

@JsonSerializable()
class DirectionDuration {
  final String text;
  final int value;

  DirectionDuration({required this.text, required this.value});

  factory DirectionDuration.fromJson(Map<String, dynamic> json) =>
      _$DirectionDurationFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionDurationToJson(this);
}

@JsonSerializable()
class DirectionStep {
  final DirectionDistance distance;
  final DirectionDuration duration;
  @JsonKey(name: 'end_location')
  final DirectionLatLng endLocation;
  @JsonKey(name: 'html_instructions')
  final String htmlInstructions;
  final DirectionPolyline polyline;
  @JsonKey(name: 'start_location')
  final DirectionLatLng startLocation;
  @JsonKey(name: 'travel_mode')
  final String travelMode;

  DirectionStep({
    required this.distance,
    required this.duration,
    required this.endLocation,
    required this.htmlInstructions,
    required this.polyline,
    required this.startLocation,
    required this.travelMode,
  });

  factory DirectionStep.fromJson(Map<String, dynamic> json) =>
      _$DirectionStepFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionStepToJson(this);
}

@JsonSerializable()
class DirectionPolyline {
  final String points;

  DirectionPolyline({required this.points});

  factory DirectionPolyline.fromJson(Map<String, dynamic> json) =>
      _$DirectionPolylineFromJson(json);

  Map<String, dynamic> toJson() => _$DirectionPolylineToJson(this);
}
