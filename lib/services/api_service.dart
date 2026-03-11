import 'package:dio/dio.dart';
import '../config/api_config.dart';

class ApiService {
  static String get baseUrl => ApiConfig.mainApiBaseUrl;

  static final Dio dio = Dio(
    BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ),
  );
}