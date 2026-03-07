import 'package:dio/dio.dart';
import '../models/login_response.dart';
import '../services/api_service.dart';

class AuthRepository {
  final Dio _dio = ApiService.dio;

  Future<LoginResponse> login({
    required String phoneNumber,
    required String password,
  }) async {
    final response = await _dio.post(
      '/auth/login',
      data: {
        'phoneNumber': phoneNumber,
        'password': password,
      },
    );

    return LoginResponse.fromJson(response.data);
  }
}