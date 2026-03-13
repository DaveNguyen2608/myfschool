// import 'dart:convert';
// import 'package:http/http.dart' as http;
//
// import '../config/api_config.dart';
// import '../models/announcement_item.dart';
//
// class AnnouncementService {
//   Future<List<AnnouncementItem>> getAnnouncements() async {
//     final url = Uri.parse('${ApiConfig.baseUrl}/Announcements');
//
//     final response = await http.get(url);
//
//     if (response.statusCode == 200) {
//       final List data = jsonDecode(response.body);
//       return data.map((e) => AnnouncementItem.fromJson(e)).toList();
//     } else {
//       throw Exception('Không tải được danh sách thông báo');
//     }
//   }
// }