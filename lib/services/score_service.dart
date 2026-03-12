import 'package:dio/dio.dart';
import '../config/api_config.dart';
import '../models/academic_year_item.dart';
import '../models/score_item.dart';
import '../models/score_summary.dart';

class ScoreService {
  late final Dio _dio;

  ScoreService() {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConfig.clubApiBaseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    _dio.interceptors.add(
      LogInterceptor(
        request: true,
        requestHeader: true,
        requestBody: true,
        responseBody: true,
        error: true,
      ),
    );
  }

  Future<List<AcademicYearItem>> getAcademicYears() async {
    final response = await _dio.get('/scores/academic-years');
    final List data = response.data as List;
    return data
        .map((e) => AcademicYearItem.fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }

  Future<ScoreSummary> getScoreSummary({
    required String username,
    required int academicYearId,
    required int semesterNo,
  }) async {
    final response = await _dio.get(
      '/scores/summary',
      queryParameters: {
        'username': username,
        'academicYearId': academicYearId,
        'semesterNo': semesterNo,
      },
    );

    return ScoreSummary.fromJson(Map<String, dynamic>.from(response.data));
  }

  Future<List<ScoreItem>> getScores({
    required String username,
    required int academicYearId,
    required int semesterNo,
  }) async {
    final response = await _dio.get(
      '/scores',
      queryParameters: {
        'username': username,
        'academicYearId': academicYearId,
        'semesterNo': semesterNo,
      },
    );

    final List data = response.data as List;
    return data
        .map((e) => ScoreItem.fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }
}