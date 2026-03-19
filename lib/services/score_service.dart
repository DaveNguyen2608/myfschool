import 'package:dio/dio.dart';
import '../config/api_config.dart';
import '../models/academic_year_item.dart';
import '../models/score_item.dart';
import '../models/score_summary.dart';
import '../models/teacher_gradebook.dart';
import '../models/teacher_score_meta.dart';
import '../models/teacher_summary_item.dart';

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

  Future<TeacherScoreMeta> getTeacherMeta({
    required String username,
    int? academicYearId,
  }) async {
    final response = await _dio.get(
      '/scores/teacher/meta',
      queryParameters: {
        'username': username,
        if (academicYearId != null) 'academicYearId': academicYearId,
      },
    );

    return TeacherScoreMeta.fromJson(Map<String, dynamic>.from(response.data));
  }

  Future<TeacherGradebook> getTeacherGradebook({
    required String username,
    required int academicYearId,
    required int semesterId,
    required int subjectId,
    required int categoryId,
  }) async {
    final response = await _dio.get(
      '/scores/teacher/gradebook',
      queryParameters: {
        'username': username,
        'academicYearId': academicYearId,
        'semesterId': semesterId,
        'subjectId': subjectId,
        'categoryId': categoryId,
      },
    );

    return TeacherGradebook.fromJson(Map<String, dynamic>.from(response.data));
  }

  Future<List<TeacherSummaryItem>> getTeacherSummary({
    required String username,
    required int academicYearId,
    int? semesterId,
  }) async {
    final response = await _dio.get(
      '/scores/teacher/summary',
      queryParameters: {
        'username': username,
        'academicYearId': academicYearId,
        if (semesterId != null) 'semesterId': semesterId,
      },
    );

    final map = Map<String, dynamic>.from(response.data);
    final rows = (map['rows'] as List<dynamic>? ?? const [])
        .map((e) => TeacherSummaryItem.fromJson(Map<String, dynamic>.from(e)))
        .toList();

    return rows;
  }

  Future<void> createTeacherGrade({
    required String username,
    required int academicYearId,
    required int studentId,
    required int semesterId,
    required int subjectId,
    required int categoryId,
    required String gradeType,
    required double score,
    String? note,
  }) async {
    await _dio.post(
      '/scores/teacher/grades',
      data: {
        'username': username,
        'academicYearId': academicYearId,
        'studentId': studentId,
        'semesterId': semesterId,
        'subjectId': subjectId,
        'categoryId': categoryId,
        'gradeType': gradeType,
        'score': score,
        'note': note,
      },
    );
  }

  Future<void> updateTeacherGrade({
    required int gradeId,
    required String username,
    required double score,
    String? note,
  }) async {
    await _dio.put(
      '/scores/teacher/grades/$gradeId',
      data: {
        'username': username,
        'score': score,
        'note': note,
      },
    );
  }
}
