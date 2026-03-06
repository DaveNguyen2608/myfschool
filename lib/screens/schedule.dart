import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import '../models/schedule_item.dart';
import '../services/api_service.dart';

class SchedulePage extends StatefulWidget {
  final String username;

  const SchedulePage({
    super.key,
    required this.username,
  });

  @override
  State<SchedulePage> createState() => _SchedulePageState();
}

class _SchedulePageState extends State<SchedulePage> {
  final orange = const Color(0xFFFF7A00);

  final List<String> _dow = const ["CN", "T2", "T3", "T4", "T5", "T6", "T7"];
  final List<int> _days = const [16, 17, 18, 19, 20, 21, 22];

  int _selectedIndex = 1;
  bool _loading = true;
  String? _error;

  List<ScheduleItem> _allItems = [];

  @override
  void initState() {
    super.initState();
    _loadSchedule();
  }

  Future<void> _loadSchedule() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final response = await ApiService.dio.get(
        '/api/Schedule/weekly',
        queryParameters: {
          'username': widget.username,
        },
      );

      final data = response.data['data'] as List<dynamic>;
      _allItems = data
          .map((e) => ScheduleItem.fromJson(e as Map<String, dynamic>))
          .toList();

      setState(() {
        _loading = false;
      });
    } on DioException catch (e) {
      String message = 'Không tải được lịch học';

      final data = e.response?.data;
      if (data is Map<String, dynamic> && data['message'] != null) {
        message = data['message'].toString();
      }

      setState(() {
        _loading = false;
        _error = message;
      });
    } catch (e) {
      setState(() {
        _loading = false;
        _error = 'Có lỗi xảy ra';
      });
    }
  }

  int get _selectedDayOfWeek {
    if (_selectedIndex == 0) return 7; // CN
    return _selectedIndex; // T2=1, T3=2, ...
  }

  List<ScheduleItem> get _periodsOfSelectedDay {
    return _allItems.where((e) => e.dayOfWeek == _selectedDayOfWeek).toList()
      ..sort((a, b) => a.periodNo.compareTo(b.periodNo));
  }

  String _periodLabel(int periodNo) => "Tiết $periodNo";

  @override
  Widget build(BuildContext context) {
    final periods = _periodsOfSelectedDay;

    return Scaffold(
      backgroundColor: const Color(0xFFF6F7FB),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: Colors.black87),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          "Lịch học",
          style: TextStyle(color: Colors.black87, fontWeight: FontWeight.w700),
        ),
        centerTitle: true,
      ),
      body: Column(
        children: [
          Container(
            color: Colors.white,
            padding: const EdgeInsets.fromLTRB(14, 10, 14, 14),
            child: Column(
              children: [
                Row(
                  children: List.generate(_dow.length, (i) {
                    final selected = i == _selectedIndex;
                    return Expanded(
                      child: GestureDetector(
                        onTap: () => setState(() => _selectedIndex = i),
                        child: Column(
                          children: [
                            Text(
                              _dow[i],
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                                color: selected ? orange : Colors.black45,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Container(
                              width: 30,
                              height: 30,
                              alignment: Alignment.center,
                              decoration: BoxDecoration(
                                color: selected ? orange : const Color(0xFFF1F2F6),
                                shape: BoxShape.circle,
                              ),
                              child: Text(
                                "${_days[i]}",
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w800,
                                  color: selected ? Colors.white : Colors.black87,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  }),
                ),
                const SizedBox(height: 10),
                Align(
                  alignment: Alignment(
                    -1 + (_selectedIndex * (2 / 6)),
                    0,
                  ),
                  child: Container(
                    width: 38,
                    height: 4,
                    decoration: BoxDecoration(
                      color: orange,
                      borderRadius: BorderRadius.circular(999),
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _error != null
                ? Center(
              child: Text(
                _error!,
                style: const TextStyle(color: Colors.red),
              ),
            )
                : periods.isEmpty
                ? const Center(
              child: Text(
                'Không có lịch học',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
            )
                : ListView.separated(
              padding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
              itemBuilder: (context, index) {
                final p = periods[index];
                return Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    SizedBox(
                      width: 86,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            _periodLabel(p.periodNo),
                            style: const TextStyle(
                              fontWeight: FontWeight.w800,
                              color: Colors.black87,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            "${p.startTime} - ${p.endTime}",
                            style: const TextStyle(
                              fontSize: 12,
                              color: Colors.black45,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _SubjectCard(
                        title: p.subjectName,
                        room: p.roomName,
                        teacher: p.teacherName,
                        orange: orange,
                      ),
                    ),
                  ],
                );
              },
              separatorBuilder: (_, __) => const SizedBox(height: 14),
              itemCount: periods.length,
            ),
          ),
        ],
      ),
    );
  }
}

class _SubjectCard extends StatelessWidget {
  final String title;
  final String room;
  final String teacher;
  final Color orange;

  const _SubjectCard({
    required this.title,
    required this.room,
    required this.teacher,
    required this.orange,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(14, 12, 12, 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        boxShadow: const [
          BoxShadow(
            blurRadius: 12,
            offset: Offset(0, 4),
            color: Color(0x12000000),
          ),
        ],
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Text(
                  title,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    color: Color(0xFF1E2A3A),
                  ),
                ),
                const SizedBox(height: 6),
                RichText(
                  textAlign: TextAlign.center,
                  text: TextSpan(
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
                    children: [
                      TextSpan(
                        text: room,
                        style: const TextStyle(color: Color(0xFF1E2A3A)),
                      ),
                      const TextSpan(
                        text: " - ",
                        style: TextStyle(color: Color(0xFF1E2A3A)),
                      ),
                      TextSpan(
                        text: teacher,
                        style: TextStyle(color: orange),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          Container(
            width: 26,
            height: 26,
            decoration: BoxDecoration(
              color: orange,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(Icons.more_horiz_rounded, color: Colors.white, size: 18),
          ),
        ],
      ),
    );
  }
}