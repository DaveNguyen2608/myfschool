import 'package:dio/dio.dart';

class ApiService {
  static final Dio dio = Dio(
    BaseOptions(
      baseUrl: 'http://localhost:5203/api',
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ),
  );
}