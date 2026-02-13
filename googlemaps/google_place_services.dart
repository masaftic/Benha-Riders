import 'package:dio/dio.dart';
import 'package:easy_go/core/networking/api_constants.dart';
import 'package:easy_go/user_features/trip_request/data/models/auto_complete_request_response.dart';
import 'package:easy_go/user_features/trip_request/data/models/geocode_response.dart';
import 'package:easy_go/user_features/trip_request/data/models/place_details_response.dart';
import 'package:retrofit/error_logger.dart';
import 'package:retrofit/http.dart';
part 'google_place_services.g.dart';

@RestApi(baseUrl: ApiConstants.googleMapsBaseUrl)
abstract class GooglePlaceServices {
  factory GooglePlaceServices(Dio dio) = _GooglePlaceServices;

  @GET('geocode/json')
  Future<GeocodeResponse> reverseGeocodeRequest(
    @Query('latlng') String latLng,
    @Query('language') String language,
  );

  @GET('place/autocomplete/json')
  Future<SearchAutoCompleteResponse> autoCompleteRequest(
    @Query('input') String input,
    @Query('language') String language,
    @Query('location') String location,
    @Query('components') String components,
    @Query('radius') int radius,
    @Query('types') String types,
  );

  @GET('place/details/json')
  Future<PlaceDetailsResponse> placeDetailsRequest(
    @Query('place_id') String placeId,
    @Query('language') String language,
    @Query('fields') String fields,
  );
}
