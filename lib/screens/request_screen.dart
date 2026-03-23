import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../models/request_item_model.dart';
import '../services/request_service.dart';

enum UserViewMode { parent, teacher }

enum LeaveType { oneDay, multiDay }

class RequestScreen extends StatefulWidget {
  final UserViewMode mode;
  final String username;

  const RequestScreen({
    super.key,
    required this.mode,
    required this.username,
  });

  @override
  State<RequestScreen> createState() => _RequestScreenState();
}

class _RequestScreenState extends State<RequestScreen> {
  static const Color orange = Color(0xFFFF7A00);
  static const Color bg = Color(0xFFF5F5F5);

  final RequestService _service = RequestService();
  bool _isLoading = true;
  String? _error;
  List<RequestItemModel> _items = const [];

  bool get _isParent => widget.mode == UserViewMode.parent;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final data = _isParent
          ? await _service.getParentRequests(widget.username)
          : await _service.getTeacherRequests(username: widget.username);

      if (!mounted) return;
      setState(() {
        _items = data;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      final message = _messageFromError(e);
      if (silent) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message)),
        );
      } else {
        setState(() {
          _isLoading = false;
          _error = message;
        });
      }
    }
  }

  Future<void> _openCreate() async {
    final result = await Navigator.push<String>(
      context,
      MaterialPageRoute(
        builder: (_) => CreateRequestScreen(
          username: widget.username,
          service: _service,
        ),
      ),
    );

    if (!mounted || result == null) return;
    await _load(silent: true);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(result)));
  }

  Future<void> _callParent(String phone) async {
    final normalized = phone.trim();
    if (normalized.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đơn này chưa có số điện thoại phụ huynh')),
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
      SnackBar(content: Text('Không thể mở cuộc gọi tới $normalized')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: bg,
      appBar: AppBar(
        backgroundColor: bg,
        elevation: 0,
        centerTitle: true,
        title: Text(
          _isParent ? 'Đơn xin phép' : 'Xem đơn',
          style: const TextStyle(
            color: Colors.black87,
            fontWeight: FontWeight.w700,
          ),
        ),
        actions: _isParent
            ? [
                IconButton(
                  onPressed: _openCreate,
                  icon: const Icon(Icons.add_circle_outline, color: orange),
                ),
              ]
            : null,
      ),
      body: Column(
        children: [
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => _load(silent: true),
              child: _buildBody(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return ListView(
        children: const [
          SizedBox(height: 120),
          Center(child: CircularProgressIndicator()),
        ],
      );
    }

    if (_error != null) {
      return ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(_error!, style: const TextStyle(color: Colors.red)),
          const SizedBox(height: 8),
          ElevatedButton(
            onPressed: _load,
            child: const Text('Thử lại'),
          ),
        ],
      );
    }

    if (_items.isEmpty) {
      return ListView(
        children: [
          const SizedBox(height: 140),
          Icon(Icons.description_outlined, size: 56, color: Colors.grey.shade400),
          const SizedBox(height: 8),
          Center(
            child: Text(
              _isParent ? 'Chưa có đơn nào' : 'Hiện chưa có đơn nghỉ học',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
        ],
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      itemCount: _items.length,
      separatorBuilder: (_, __) => const SizedBox(height: 10),
      itemBuilder: (_, index) {
        final item = _items[index];

        return Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(16),
            boxShadow: const [
              BoxShadow(
                blurRadius: 8,
                offset: Offset(0, 2),
                color: Color(0x14000000),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.description_outlined, color: orange, size: 19),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      item.studentName,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text('Lớp: ${item.className.isEmpty ? '--' : item.className}'),
              Text('Loại đơn: ${_typeText(item.requestTypeCode)}'),
              Text('Thời gian: ${_fmt(item.startDate)} - ${_fmt(item.endDate)}'),
              if (!_isParent) ...[
                const SizedBox(height: 6),
                Text('Phụ huynh: ${item.parentName.isEmpty ? '--' : item.parentName}'),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        'Số điện thoại: ${item.parentPhone.isEmpty ? '--' : item.parentPhone}',
                      ),
                    ),
                    IconButton(
                      onPressed: item.parentPhone.isEmpty
                          ? null
                          : () => _callParent(item.parentPhone),
                      icon: const Icon(Icons.phone, color: Colors.green),
                      tooltip: 'Gọi phụ huynh',
                    ),
                  ],
                ),
              ],
              const SizedBox(height: 6),
              Text(item.reason, style: const TextStyle(color: Colors.black54)),
            ],
          ),
        );
      },
    );
  }

  static String _fmt(DateTime d) {
    final day = d.day.toString().padLeft(2, '0');
    final month = d.month.toString().padLeft(2, '0');
    return '$day/$month/${d.year}';
  }

  static String _typeText(String code) {
    if (code.toUpperCase() == 'LEAVE_ONE_DAY') return 'Xin nghỉ học 1 ngày';
    if (code.toUpperCase() == 'LEAVE_MULTI_DAY') return 'Xin nghỉ học dài ngày';
    return code;
  }
}

class CreateRequestScreen extends StatefulWidget {
  final String username;
  final RequestService service;

  const CreateRequestScreen({
    super.key,
    required this.username,
    required this.service,
  });

  @override
  State<CreateRequestScreen> createState() => _CreateRequestScreenState();
}

class _CreateRequestScreenState extends State<CreateRequestScreen> {
  static const Color orange = Color(0xFFFF7A00);

  List<ParentStudentOption> _students = const [];
  ParentStudentOption? _selected;
  LeaveType _type = LeaveType.oneDay;
  DateTime _start = DateTime.now();
  DateTime _end = DateTime.now();
  bool _isLoading = true;
  bool _isSubmitting = false;

  final TextEditingController _reason = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadStudents();
  }

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  Future<void> _loadStudents() async {
    try {
      final data = await widget.service.getParentStudents(widget.username);
      if (!mounted) return;
      setState(() {
        _students = data;
        _selected = data.isNotEmpty ? data.first : null;
        _isLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _isLoading = false);
    }
  }

  Future<void> _submit() async {
    if (_selected == null) return;
    if (_reason.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập lý do')),
      );
      return;
    }

    if (_type == LeaveType.oneDay) {
      _end = _start;
    }

    if (_end.isBefore(_start)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu')),
      );
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      final msg = await widget.service.createParentRequest(
        username: widget.username,
        studentId: _selected!.studentId,
        requestTypeCode: _type == LeaveType.oneDay ? 'LEAVE_ONE_DAY' : 'LEAVE_MULTI_DAY',
        startDate: _start,
        endDate: _end,
        reason: _reason.text.trim(),
      );

      if (!mounted) return;
      Navigator.pop(context, msg);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(_messageFromError(e))),
      );
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Tạo đơn')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  DropdownButtonFormField<ParentStudentOption>(
                    value: _selected,
                    decoration: const InputDecoration(labelText: 'Học sinh'),
                    items: _students
                        .map((s) => DropdownMenuItem(value: s, child: Text(s.studentName)))
                        .toList(),
                    onChanged: (v) => setState(() => _selected = v),
                  ),
                  const SizedBox(height: 8),
                  Text('Lớp: ${_selected?.className ?? '--'}'),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      ChoiceChip(
                        label: const Text('1 ngày'),
                        selected: _type == LeaveType.oneDay,
                        selectedColor: orange.withOpacity(0.2),
                        onSelected: (_) {
                          setState(() {
                            _type = LeaveType.oneDay;
                            _end = _start;
                          });
                        },
                      ),
                      const SizedBox(width: 8),
                      ChoiceChip(
                        label: const Text('Dài ngày'),
                        selected: _type == LeaveType.multiDay,
                        selectedColor: orange.withOpacity(0.2),
                        onSelected: (_) {
                          setState(() {
                            _type = LeaveType.multiDay;
                          });
                        },
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Từ ngày'),
                    subtitle: Text(_RequestScreenState._fmt(_start)),
                    trailing: const Icon(Icons.calendar_today_rounded),
                    onTap: () async {
                      final p = await showDatePicker(
                        context: context,
                        initialDate: _start,
                        firstDate: DateTime(2024),
                        lastDate: DateTime(2035),
                      );
                      if (p == null) return;
                      setState(() {
                        _start = DateTime(p.year, p.month, p.day);
                        if (_type == LeaveType.oneDay || _end.isBefore(_start)) {
                          _end = _start;
                        }
                      });
                    },
                  ),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Đến ngày'),
                    subtitle: Text(_RequestScreenState._fmt(_end)),
                    trailing: const Icon(Icons.calendar_today_rounded),
                    onTap: _type == LeaveType.oneDay
                        ? null
                        : () async {
                            final p = await showDatePicker(
                              context: context,
                              initialDate: _end.isBefore(_start) ? _start : _end,
                              firstDate: _start,
                              lastDate: DateTime(2035),
                            );
                            if (p == null) return;
                            setState(() => _end = DateTime(p.year, p.month, p.day));
                          },
                  ),
                  TextField(
                    controller: _reason,
                    minLines: 3,
                    maxLines: 4,
                    decoration: const InputDecoration(labelText: 'Lý do'),
                  ),
                  const SizedBox(height: 16),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      style: ElevatedButton.styleFrom(backgroundColor: orange),
                      onPressed: _isSubmitting ? null : _submit,
                      child: _isSubmitting
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Text(
                              'Gửi đơn',
                              style: TextStyle(color: Colors.white),
                            ),
                    ),
                  ),
                ],
              ),
            ),
    );
  }
}

String _messageFromError(Object error) {
  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map<String, dynamic> && data['message'] != null) {
      return data['message'].toString();
    }
  }
  return 'Có lỗi xảy ra, vui lòng thử lại';
}
