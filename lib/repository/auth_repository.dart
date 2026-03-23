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

  Future<Map<String, dynamic>> sendForgotPasswordOtp({
    required String phoneNumber,
  }) async {
    final response = await _dio.post(
      '/auth/forgot-password/send-otp',
      data: {
        'phoneNumber': phoneNumber,
      },
    );

    if (response.data is Map<String, dynamic>) {
      return Map<String, dynamic>.from(response.data as Map<String, dynamic>);
    }

    return {'message': 'Đã gửi OTP'};
  }

  Future<String> resetPasswordByOtp({
    required String phoneNumber,
    required String otpCode,
    required String newPassword,
    required String confirmPassword,
  }) async {
    final response = await _dio.post(
      '/auth/forgot-password/reset-by-otp',
      data: {
        'phoneNumber': phoneNumber,
        'otpCode': otpCode,
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      },
    );

    final data = response.data;
    if (data is Map<String, dynamic> && data['message'] != null) {
      return data['message'].toString();
    }

    return 'Đặt lại mật khẩu thành công';
  }
}
