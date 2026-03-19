import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../models/schedule_item.dart';
import '../services/api_service.dart';
import 'contact_screen.dart';

class SchedulePage extends StatefulWidget {
  final String username;
  final String displayName;
  final String className;
  final bool isTeacher;

  const SchedulePage({
    super.key,
    required this.username,
    this.displayName = '',
    this.className = '',
    this.isTeacher = false,
  });

  @override
  State<SchedulePage> createState() => _SchedulePageState();
}

class _SchedulePageState extends State<SchedulePage> {
  static final DateTime _fallbackAcademicStart = DateTime(2025, 9, 5);
  static final DateTime _fallbackAcademicEnd = DateTime(2026, 5, 31);

  final Color orange = const Color(0xFFFF7A00);
  final List<String> _dowLabels = const ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];

  DateTime _selectedDate = DateUtils.dateOnly(DateTime.now());
  DateTime _academicStartDate = _fallbackAcademicStart;
  DateTime _academicEndDate = _fallbackAcademicEnd;

  bool _loading = true;
  String? _error;

  bool _isInAcademicYear = true;
  bool _isHoliday = false;
  String _holidayTitle = '';
  String _holidayDescription = '';

  String _academicYearName = '';
  int? _classId;

  List<ScheduleItem> _periods = const [];

  @override
  void initState() {
    super.initState();
    _loadScheduleByDate(_selectedDate);
  }

  List<DateTime> get _weekDates {
    final start = _startOfWeek(_selectedDate);
    return List<DateTime>.generate(7, (index) => start.add(Duration(days: index)));
  }

  int get _selectedIndexInWeek => _selectedDate.weekday % 7;

  Future<void> _loadScheduleByDate(DateTime date, {bool showLoading = true}) async {
    if (showLoading) {
      setState(() {
        _loading = true;
        _error = null;
      });
    }

    final selected = DateUtils.dateOnly(date);

    try {
      final query = <String, dynamic>{
        'username': widget.username,
        'date': _toApiDate(selected),
      };
      if (_classId != null) {
        query['classId'] = _classId;
      }

      final response = await ApiService.dio.get('/Schedule/by-date', queryParameters: query);
      final payload = _toJsonMap(response.data);

      final periods = _toJsonList(payload['periods'])
          .map((e) => ScheduleItem.fromJson(e))
          .toList()
        ..sort((a, b) => a.periodNo.compareTo(b.periodNo));

      final resolvedDate = _parseDate(payload['selectedDate']) ?? selected;
      final resolvedAcademicStart = _parseDate(payload['academicYearStartDate']) ?? _fallbackAcademicStart;
      final resolvedAcademicEnd = _parseDate(payload['academicYearEndDate']) ?? _fallbackAcademicEnd;

      if (!mounted) return;
      setState(() {
        _selectedDate = resolvedDate;
        _academicStartDate = resolvedAcademicStart;
        _academicEndDate = resolvedAcademicEnd;
        _isInAcademicYear = payload['isInAcademicYear'] == true;
        _isHoliday = payload['isHoliday'] == true;
        _holidayTitle = payload['holidayTitle']?.toString() ?? '';
        _holidayDescription = payload['holidayDescription']?.toString() ?? '';
        _academicYearName = payload['academicYear']?.toString() ?? '';
        _classId = _toIntOrNull(payload['classId']);
        _periods = periods;
        _loading = false;
        _error = null;
      });
    } on DioException catch (e) {
      var message = 'Không tải được lịch học theo ngày';
      final data = e.response?.data;
      if (data is Map && data['message'] != null) {
        message = data['message'].toString();
      }

      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = message;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = 'Có lỗi xảy ra';
      });
    }
  }

  Future<void> _pickDate() async {
    final firstDate = _academicStartDate.subtract(const Duration(days: 180));
    final lastDate = _academicEndDate.add(const Duration(days: 180));

    final initialDate = _selectedDate.isBefore(firstDate)
        ? firstDate
        : _selectedDate.isAfter(lastDate)
            ? lastDate
            : _selectedDate;

    final picked = await showDatePicker(
      context: context,
      initialDate: initialDate,
      firstDate: firstDate,
      lastDate: lastDate,
      helpText: 'Chọn ngày xem lịch học',
      cancelText: 'Hủy',
      confirmText: 'Chọn',
    );

    if (picked == null) {
      return;
    }

    await _loadScheduleByDate(picked);
  }

  Future<void> _onTapWeekDate(DateTime date) async {
    await _loadScheduleByDate(date);
  }

  String _periodLabel(int periodNo) => 'Tiết $periodNo';

  Future<void> _onMoreTap(ScheduleItem item) async {
    final hasContact = item.teacherId > 0 &&
        (item.teacherName.trim().isNotEmpty ||
            item.teacherPhone.trim().isNotEmpty ||
            item.teacherEmail.trim().isNotEmpty);

    if (!hasContact) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Chưa có thông tin liên lạc giáo viên')),
      );
      return;
    }

    if (!mounted) return;

    await showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      isScrollControlled: true,
      builder: (_) => _TeacherContactSheet(
        item: item,
        onCall: _callPhone,
        onEmail: _sendEmail,
        onOpenFull: () async {
          Navigator.pop(context);
          await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => ContactScreen(
                username: widget.username,
                studentName: widget.displayName,
                className: widget.className,
                isTeacher: widget.isTeacher,
              ),
            ),
          );
        },
      ),
    );
  }

  Future<void> _callPhone(String phone) async {
    final normalized = phone.trim();
    if (normalized.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Giáo viên chưa có số điện thoại')),
      );
      return;
    }

    final uri = Uri(scheme: 'tel', path: normalized);
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri);
      return;
    }

    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Không thể gọi tới $normalized')),
    );
  }

  Future<void> _sendEmail(String email) async {
    final normalized = email.trim();
    if (normalized.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Giáo viên chưa có email')),
      );
      return;
    }

    final uri = Uri(scheme: 'mailto', path: normalized);
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri);
      return;
    }

    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Không thể mở email $normalized')),
    );
  }

  @override
  Widget build(BuildContext context) {
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
          'Lịch học',
          style: TextStyle(color: Colors.black87, fontWeight: FontWeight.w700),
        ),
        centerTitle: true,
      ),
      body: Column(
        children: [
          _buildDateHeader(),
          const SizedBox(height: 10),
          Expanded(child: _buildContent()),
        ],
      ),
    );
  }

  Widget _buildDateHeader() {
    final weekDates = _weekDates;

    return Container(
      color: Colors.white,
      padding: const EdgeInsets.fromLTRB(14, 10, 14, 14),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  'Ngày ${_formatDate(_selectedDate)}',
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: Color(0xFF1E2A3A),
                  ),
                ),
              ),
              InkWell(
                borderRadius: BorderRadius.circular(10),
                onTap: _pickDate,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFFF1E6),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.calendar_month_rounded, size: 18, color: orange),
                      const SizedBox(width: 6),
                      Text(
                        'Chọn ngày',
                        style: TextStyle(
                          color: orange,
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: List.generate(weekDates.length, (index) {
              final date = weekDates[index];
              final selected = DateUtils.isSameDay(date, _selectedDate);

              return Expanded(
                child: GestureDetector(
                  onTap: () => _onTapWeekDate(date),
                  child: Column(
                    children: [
                      Text(
                        _dowLabels[index],
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
                          '${date.day}',
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
            alignment: Alignment(-1 + (_selectedIndexInWeek * (2 / 6)), 0),
            child: Container(
              width: 38,
              height: 4,
              decoration: BoxDecoration(
                color: orange,
                borderRadius: BorderRadius.circular(999),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerLeft,
            child: Text(
              _buildAcademicRangeText(),
              style: const TextStyle(
                fontSize: 12,
                color: Colors.black54,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContent() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Text(
            _error!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: Colors.red),
          ),
        ),
      );
    }

    if (!_isInAcademicYear) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Text(
            'Ngày đã chọn ngoài phạm vi năm học (${_formatDate(_academicStartDate)} - ${_formatDate(_academicEndDate)})',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Colors.red,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      );
    }

    if (_isHoliday) {
      final subtitle = _holidayDescription.trim().isNotEmpty
          ? _holidayDescription.trim()
          : 'Hôm nay nghỉ học theo lịch nhà trường';

      return Center(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.event_busy_rounded, size: 42, color: Colors.orange),
              const SizedBox(height: 10),
              Text(
                _holidayTitle.trim().isNotEmpty ? _holidayTitle : 'Hôm nay nghỉ học',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF1E2A3A),
                ),
              ),
              const SizedBox(height: 6),
              Text(
                subtitle,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Colors.black54,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      );
    }

    if (_periods.isEmpty) {
      return const Center(
        child: Text(
          'Không có lịch học',
          style: TextStyle(fontWeight: FontWeight.w600),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
      itemBuilder: (context, index) {
        final period = _periods[index];
        final teacherAlias = period.teacherAlias.trim().isNotEmpty
            ? period.teacherAlias
            : period.teacherName;

        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 86,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _periodLabel(period.periodNo),
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      color: Colors.black87,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${period.startTime} - ${period.endTime}',
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
                title: period.subjectName,
                room: period.roomName,
                teacherAlias: teacherAlias,
                orange: orange,
                onMoreTap: () => _onMoreTap(period),
              ),
            ),
          ],
        );
      },
      separatorBuilder: (_, __) => const SizedBox(height: 14),
      itemCount: _periods.length,
    );
  }

  String _buildAcademicRangeText() {
    final range = 'Năm học: ${_formatDate(_academicStartDate)} - ${_formatDate(_academicEndDate)}';
    if (_academicYearName.trim().isEmpty) {
      return range;
    }
    return '${_academicYearName.trim()} · $range';
  }

  static DateTime _startOfWeek(DateTime date) {
    final clean = DateUtils.dateOnly(date);
    final diff = clean.weekday % 7;
    return clean.subtract(Duration(days: diff));
  }

  static String _toApiDate(DateTime date) {
    final clean = DateUtils.dateOnly(date);
    final y = clean.year.toString().padLeft(4, '0');
    final m = clean.month.toString().padLeft(2, '0');
    final d = clean.day.toString().padLeft(2, '0');
    return '$y-$m-$d';
  }

  static String _formatDate(DateTime date) {
    final clean = DateUtils.dateOnly(date);
    final d = clean.day.toString().padLeft(2, '0');
    final m = clean.month.toString().padLeft(2, '0');
    final y = clean.year.toString().padLeft(4, '0');
    return '$d/$m/$y';
  }

  static DateTime? _parseDate(dynamic value) {
    if (value == null) {
      return null;
    }

    final text = value.toString().trim();
    if (text.isEmpty) {
      return null;
    }

    final parsed = DateTime.tryParse(text);
    if (parsed == null) {
      return null;
    }

    return DateUtils.dateOnly(parsed);
  }

  static Map<String, dynamic> _toJsonMap(dynamic value) {
    if (value is Map<String, dynamic>) {
      return value;
    }

    if (value is Map) {
      return value.map((key, data) => MapEntry(key.toString(), data));
    }

    return {};
  }

  static List<Map<String, dynamic>> _toJsonList(dynamic value) {
    if (value is! List) {
      return const [];
    }

    return value
        .whereType<Map>()
        .map((item) => item.map((key, data) => MapEntry(key.toString(), data)))
        .toList();
  }

  static int? _toIntOrNull(dynamic value) {
    if (value is int) {
      return value;
    }

    final parsed = int.tryParse(value?.toString() ?? '');
    return parsed;
  }
}

class _SubjectCard extends StatelessWidget {
  final String title;
  final String room;
  final String teacherAlias;
  final Color orange;
  final VoidCallback onMoreTap;

  const _SubjectCard({
    required this.title,
    required this.room,
    required this.teacherAlias,
    required this.orange,
    required this.onMoreTap,
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
                        text: ' - ',
                        style: TextStyle(color: Color(0xFF1E2A3A)),
                      ),
                      TextSpan(
                        text: teacherAlias,
                        style: TextStyle(color: orange),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          InkWell(
            onTap: onMoreTap,
            borderRadius: BorderRadius.circular(8),
            child: Container(
              width: 26,
              height: 26,
              decoration: BoxDecoration(
                color: orange,
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Icon(Icons.more_horiz_rounded, color: Colors.white, size: 18),
            ),
          ),
        ],
      ),
    );
  }
}

class _TeacherContactSheet extends StatelessWidget {
  final ScheduleItem item;
  final Future<void> Function(String phone) onCall;
  final Future<void> Function(String email) onEmail;
  final Future<void> Function() onOpenFull;

  const _TeacherContactSheet({
    required this.item,
    required this.onCall,
    required this.onEmail,
    required this.onOpenFull,
  });

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);
    final alias = item.teacherAlias.trim().isNotEmpty ? item.teacherAlias : item.teacherName;

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(height: 10),
            Container(
              width: 44,
              height: 5,
              decoration: BoxDecoration(
                color: const Color(0xFFE0E0E0),
                borderRadius: BorderRadius.circular(999),
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: Text(
                    alias,
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF222222),
                    ),
                  ),
                ),
                if (item.teacherRole.trim().isNotEmpty)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF1E6),
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Text(
                      item.teacherRole,
                      style: const TextStyle(
                        color: orange,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 12),
            _ContactRow(icon: Icons.badge_outlined, label: 'Alias', value: alias),
            _ContactRow(icon: Icons.person_outline, label: 'Tên thật', value: item.teacherName),
            _ContactRow(icon: Icons.book_outlined, label: 'Môn học', value: item.subjectName),
            _ContactRow(icon: Icons.room_outlined, label: 'Phòng học', value: item.roomName),
            _ContactRow(icon: Icons.phone_outlined, label: 'Số điện thoại', value: item.teacherPhone),
            _ContactRow(icon: Icons.email_outlined, label: 'Email', value: item.teacherEmail),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: SizedBox(
                    height: 46,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        elevation: 0,
                        backgroundColor: orange,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                      onPressed: () => onCall(item.teacherPhone),
                      icon: const Icon(Icons.call_rounded, size: 18),
                      label: const Text('Gọi điện', style: TextStyle(fontWeight: FontWeight.w700)),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: SizedBox(
                    height: 46,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        elevation: 0,
                        backgroundColor: const Color(0xFFFFF1E6),
                        foregroundColor: orange,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                      onPressed: () => onEmail(item.teacherEmail),
                      icon: const Icon(Icons.mail_outline_rounded, size: 18),
                      label: const Text('Email', style: TextStyle(fontWeight: FontWeight.w700)),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              height: 44,
              child: OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: orange),
                  foregroundColor: orange,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
                onPressed: onOpenFull,
                icon: const Icon(Icons.contact_page_outlined, size: 18),
                label: const Text('Đi tới màn Liên lạc', style: TextStyle(fontWeight: FontWeight.w700)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ContactRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _ContactRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Icon(icon, size: 18, color: const Color(0xFF7B7B7B)),
          const SizedBox(width: 8),
          SizedBox(
            width: 96,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 13,
                color: Color(0xFF7B7B7B),
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value.trim().isEmpty ? '--' : value,
              style: const TextStyle(
                fontSize: 13,
                color: Color(0xFF222222),
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
