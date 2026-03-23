import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import '../models/academic_year_item.dart';
import '../models/score_item.dart';
import '../models/score_summary.dart';
import '../models/teacher_gradebook.dart';
import '../models/teacher_score_meta.dart';
import '../models/teacher_summary_item.dart';
import 'api_service.dart';

class ScoreService {
  final Dio _dio = ApiService.dio;

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

  Future<List<int>> exportTeacherScores({
    required String username,
    required int classId,
    required int academicYearId,
    int? semesterId,
    String? semesterType,
  }) async {
    final response = await _dio.get<dynamic>(
      '/scores/teacher/export',
      queryParameters: {
        'username': username,
        'classId': classId,
        'academicYearId': academicYearId,
        if (semesterId != null) 'semesterId': semesterId,
        if (semesterType != null && semesterType.trim().isNotEmpty)
          'semesterType': semesterType,
      },
      options: Options(responseType: ResponseType.bytes),
    );

    return _readBinaryBytes(response.data);
  }

  Future<List<int>> downloadTeacherScoreTemplate({
    required String username,
    required int classId,
    required int academicYearId,
    int? semesterId,
    String? semesterType,
  }) async {
    final response = await _dio.get<dynamic>(
      '/scores/teacher/template',
      queryParameters: {
        'username': username,
        'classId': classId,
        'academicYearId': academicYearId,
        if (semesterId != null) 'semesterId': semesterId,
        if (semesterType != null && semesterType.trim().isNotEmpty)
          'semesterType': semesterType,
      },
      options: Options(responseType: ResponseType.bytes),
    );

    return _readBinaryBytes(response.data);
  }

  Future<Map<String, dynamic>> importTeacherScores({
    required String username,
    required int classId,
    required int academicYearId,
    String? filePath,
    List<int>? fileBytes,
    String? fileName,
    int? semesterId,
    String? semesterType,
  }) async {
    final hasFilePath = filePath != null && filePath.trim().isNotEmpty;
    final hasFileBytes = fileBytes != null && fileBytes.isNotEmpty;
    if (!hasFilePath && !hasFileBytes) {
      throw ArgumentError('Thiếu dữ liệu file import');
    }

    MultipartFile filePart;
    if (hasFileBytes) {
      filePart = MultipartFile.fromBytes(
        fileBytes,
        filename: fileName ?? 'scores.xlsx',
      );
    } else {
      final resolvedName = (fileName != null && fileName.trim().isNotEmpty)
          ? fileName.trim()
          : filePath!.split(RegExp(r'[\\\\/]')).last;
      filePart = await MultipartFile.fromFile(filePath!, filename: resolvedName);
    }

    final formData = FormData.fromMap({
      'username': username,
      'classId': classId,
      'academicYearId': academicYearId,
      if (semesterId != null) 'semesterId': semesterId,
      if (semesterType != null && semesterType.trim().isNotEmpty)
        'semesterType': semesterType,
      'file': filePart,
    });

    final response = await _dio.post(
      '/scores/teacher/import',
      data: formData,
      options: Options(contentType: 'multipart/form-data'),
    );

    return Map<String, dynamic>.from(response.data as Map);
  }

  List<int> _readBinaryBytes(dynamic data) {
    if (data == null) {
      return <int>[];
    }

    if (data is Uint8List) {
      return data.toList(growable: false);
    }

    if (data is List<int>) {
      return List<int>.from(data, growable: false);
    }

    if (data is List<dynamic>) {
      final result = <int>[];
      for (final item in data) {
        if (item is int) {
          result.add(item);
        } else if (item is num) {
          result.add(item.toInt());
        } else {
          final parsed = int.tryParse(item.toString());
          if (parsed != null) {
            result.add(parsed);
          }
        }
      }
      return result;
    }

    if (data is ByteBuffer) {
      return data.asUint8List().toList(growable: false);
    }

    if (data is String) {
      final raw = data.trim();
      if (raw.isEmpty) {
        return <int>[];
      }

      try {
        final decoded = base64Decode(raw);
        if (_looksLikeOfficeZip(decoded)) {
          return decoded;
        }
      } catch (_) {}

      try {
        final parsed = jsonDecode(raw);
        if (parsed is List) {
          final listBytes = parsed
              .map((e) => e is num ? e.toInt() : int.tryParse(e.toString()))
              .whereType<int>()
              .toList(growable: false);
          if (_looksLikeOfficeZip(listBytes)) {
            return listBytes;
          }
        }
      } catch (_) {}

      try {
        final latin = latin1.encode(raw);
        if (_looksLikeOfficeZip(latin)) {
          return latin;
        }
      } catch (_) {}

      // Trả rỗng để lớp UI báo lỗi rõ ràng thay vì tạo file .xlsx bị hỏng.
      return <int>[];
    }

    return <int>[];
  }

  bool _looksLikeOfficeZip(List<int> bytes) {
    if (bytes.length < 4) {
      return false;
    }

    // XLSX là gói ZIP, signature phải bắt đầu bằng "PK".
    return bytes[0] == 0x50 && bytes[1] == 0x4B;
  }
}

