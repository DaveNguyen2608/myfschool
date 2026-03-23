import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../config/api_config.dart';

class ApiService {
  ApiService._();

  static const String _accessTokenKey = 'auth_access_token';

  static String? _accessToken;
  static bool _initialized = false;
  static bool _interceptorsAttached = false;

  static String get baseUrl => ApiConfig.mainApiBaseUrl;

  static final Dio dio = Dio(
    BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ),
  );

  static Future<void> initialize() async {
    if (!_interceptorsAttached) {
      _attachInterceptors();
    }

    if (_initialized) {
      return;
    }

    final prefs = await SharedPreferences.getInstance();
    _accessToken = prefs.getString(_accessTokenKey);
    _initialized = true;
  }

  static Future<void> setAccessToken(String token) async {
    final normalized = token.trim();
    if (normalized.isEmpty) {
      await clearAccessToken();
      return;
    }

    _accessToken = normalized;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_accessTokenKey, normalized);
  }

  static Future<void> clearAccessToken() async {
    _accessToken = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_accessTokenKey);
  }

  static void _attachInterceptors() {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          final token = _accessToken;
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final statusCode = error.response?.statusCode;
          if (statusCode == 401) {
            await clearAccessToken();
            final data = error.response?.data;
            if (data is! Map || data['message'] == null) {
              error.response?.data = {
                'message': 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại',
              };
            }
          }
          handler.next(error);
        },
      ),
    );

    _interceptorsAttached = true;
  }
}
