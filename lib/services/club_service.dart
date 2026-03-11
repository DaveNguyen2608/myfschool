import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../models/club_item.dart';

class ClubService {
  late final Dio _dio;

  ClubService() {
    final baseUrl = kIsWeb
        ? 'http://localhost:5203/api'
        : 'http://10.0.2.2:5203/api';

    _dio = Dio(
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
  }

  Future<List<ClubItem>> getAllClubs(String username) async {
    final response = await _dio.get(
      '/clubs',
      queryParameters: {'username': username},
    );

    final List data = response.data as List;
    return data
        .map((e) => ClubItem.fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }

  Future<List<ClubItem>> getMyClubs(String username) async {
    final response = await _dio.get(
      '/clubs/my',
      queryParameters: {'username': username},
    );

    final List data = response.data as List;
    return data
        .map((e) => ClubItem.fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }

  Future<ClubItem> getClubDetail(int clubId, String username) async {
    final response = await _dio.get(
      '/clubs/$clubId',
      queryParameters: {'username': username},
    );

    return ClubItem.fromJson(Map<String, dynamic>.from(response.data));
  }

  Future<String> registerClub(int clubId, String username) async {
    final response = await _dio.post(
      '/clubs/$clubId/register',
      data: {'username': username},
    );
    return (response.data['message'] ?? 'Đăng ký thành công').toString();
  }

  Future<String> cancelClub(int clubId, String username) async {
    final response = await _dio.post(
      '/clubs/$clubId/cancel',
      data: {'username': username},
    );
    return (response.data['message'] ?? 'Hủy đăng ký thành công').toString();
  }
}