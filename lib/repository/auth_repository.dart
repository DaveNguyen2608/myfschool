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

  Future<String> changePassword({
    required String username,
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
  }) async {
    final response = await _dio.post(
      '/auth/change-password',
      data: {
        'username': username,
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      },
    );

    final data = response.data;
    if (data is Map<String, dynamic> && data['message'] != null) {
      return data['message'].toString();
    }

    return 'Đổi mật khẩu thành công';
  }
}
