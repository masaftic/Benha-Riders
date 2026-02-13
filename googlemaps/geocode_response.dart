import 'package:json_annotation/json_annotation.dart';

part 'geocode_response.g.dart';

@JsonSerializable()
class GeocodeResponse {
  @JsonKey(name: 'error_message')
  final String? errorMessage;
  final List<Results> results;
  final String status;

  GeocodeResponse({
    this.errorMessage,
    required this.results,
    required this.status,
  });
  factory GeocodeResponse.fromJson(Map<String, dynamic> json) =>
      _$GeocodeResponseFromJson(json);
}

@JsonSerializable()
class Results {
  @JsonKey(name: 'formatted_address')
  final String formattedAddress;

  Results({required this.formattedAddress});
  factory Results.fromJson(Map<String, dynamic> json) =>
      _$ResultsFromJson(json);
}
