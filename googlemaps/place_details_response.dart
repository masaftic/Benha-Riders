import 'package:json_annotation/json_annotation.dart';

part 'place_details_response.g.dart';


@JsonSerializable()
class PlaceDetailsResponse {
  final PlaceDetailsResult result;
  final String status;

  PlaceDetailsResponse({required this.result, required this.status});

  factory PlaceDetailsResponse.fromJson(Map<String, dynamic> json) =>
      _$PlaceDetailsResponseFromJson(json);
}

@JsonSerializable()
class PlaceDetailsResult {
  final String name;
  final Geometry geometry;

  PlaceDetailsResult({required this.name, required this.geometry});

  factory PlaceDetailsResult.fromJson(Map<String, dynamic> json) =>
      _$PlaceDetailsResultFromJson(json);
}

@JsonSerializable()
class Geometry {
  final Location location;

  Geometry({required this.location});

  factory Geometry.fromJson(Map<String, dynamic> json) =>
      _$GeometryFromJson(json);
}

@JsonSerializable()
class Location {
  final double lat;
  final double lng;

  Location({required this.lat, required this.lng});

  factory Location.fromJson(Map<String, dynamic> json) =>
      _$LocationFromJson(json);
}
