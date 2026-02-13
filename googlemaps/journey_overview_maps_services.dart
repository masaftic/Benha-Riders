import 'package:dio/dio.dart';
import 'package:easy_go/core/networking/api_constants.dart';

import 'package:easy_go/user_features/trip_request/data/models/directions_response.dart';
import 'package:retrofit/error_logger.dart';
import 'package:retrofit/http.dart';
part 'journey_overview_maps_services.g.dart';

@RestApi(baseUrl: ApiConstants.googleMapsBaseUrl)
abstract class JourneyOverviewMapsServices {
  factory JourneyOverviewMapsServices(Dio googleMapsDio) =
      _JourneyOverviewMapsServices;
  @GET('directions/json')
  Future<DirectionsResponse> getDirections(
    @Query('origin') String origin,
    @Query('destination') String destination,
    @Query('language') String language,
    @Query('mode') String mode,
    @Query('alternatives') bool alternatives,
  );
}
