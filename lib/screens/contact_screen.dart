import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../models/contact_item_model.dart';
import '../services/contact_service.dart';

class ContactScreen extends StatefulWidget {
  final String username;
  final String studentName;
  final String className;
  final bool isTeacher;

  const ContactScreen({
    super.key,
    required this.username,
    required this.studentName,
    required this.className,
    this.isTeacher = false,
  });

  @override
  State<ContactScreen> createState() => _ContactScreenState();
}

class _ContactScreenState extends State<ContactScreen> {
  static const Color orange = Color(0xFFFF7A00);
  static const Color bg = Color(0xFFF5F5F5);
  static const Color textDark = Color(0xFF222222);
  static const Color textGrey = Color(0xFF7B7B7B);

  final ContactService _service = ContactService();
  final TextEditingController _searchController = TextEditingController();

  int _tabIndex = 0; // 0: giáo viên, 1: nhà trường
  String _keyword = '';
  bool _isLoading = true;
  String? _error;

  List<TeacherContactItemModel> _teacherContacts = const [];
  List<SchoolContactItemModel> _schoolContacts = const [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final results = await Future.wait([
        _service.getTeacherContacts(username: widget.username),
        _service.getSchoolContacts(username: widget.username),
      ]);

      if (!mounted) return;
      setState(() {
        _teacherContacts = results[0] as List<TeacherContactItemModel>;
        _schoolContacts = results[1] as List<SchoolContactItemModel>;
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
          _error = message;
          _isLoading = false;
        });
      }
    }
  }

  List<TeacherContactItemModel> get _filteredTeachers {
    if (_keyword.trim().isEmpty) return _teacherContacts;

    final q = _keyword.trim().toLowerCase();
    return _teacherContacts.where((e) {
      return e.displayName.toLowerCase().contains(q) ||
          e.fullName.toLowerCase().contains(q) ||
          e.role.toLowerCase().contains(q) ||
          e.subjectName.toLowerCase().contains(q) ||
          e.phone.toLowerCase().contains(q);
    }).toList();
  }

  List<SchoolContactItemModel> get _filteredSchools {
    if (_keyword.trim().isEmpty) return _schoolContacts;

    final q = _keyword.trim().toLowerCase();
    return _schoolContacts.where((e) {
      return e.departmentName.toLowerCase().contains(q) ||
          e.contactName.toLowerCase().contains(q) ||
          e.phone.toLowerCase().contains(q) ||
          e.email.toLowerCase().contains(q);
    }).toList();
  }

  Future<void> _callPhone(String phone) async {
    final normalized = phone.trim();
    if (normalized.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Chưa có số điện thoại')),
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
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Chưa có địa chỉ email')),
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
    final subtitle = widget.isTeacher
        ? 'Danh bạ nội bộ lớp ${widget.className}'
        : '${widget.studentName} · ${widget.className}';

    return Scaffold(
      backgroundColor: bg,
      appBar: AppBar(
        backgroundColor: bg,
        elevation: 0,
        centerTitle: true,
        title: const Text(
          'Liên lạc',
          style: TextStyle(
            color: textDark,
            fontWeight: FontWeight.w700,
          ),
        ),
        iconTheme: const IconThemeData(color: textDark),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Column(
              children: [
                _buildHeaderCard(subtitle),
                const SizedBox(height: 14),
                _buildSearchBox(),
                const SizedBox(height: 12),
                _buildTabs(),
                const SizedBox(height: 12),
              ],
            ),
          ),
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

    return _tabIndex == 0 ? _buildTeacherList() : _buildSchoolList();
  }

  Widget _buildHeaderCard(String subtitle) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(22),
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
          Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              color: const Color(0xFFFFF1E6),
              borderRadius: BorderRadius.circular(16),
            ),
            child: const Icon(Icons.call_outlined, color: orange, size: 28),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Danh bạ liên lạc',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                    color: textDark,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  subtitle,
                  style: const TextStyle(
                    fontSize: 13,
                    color: textGrey,
                    height: 1.35,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSearchBox() {
    return Container(
      height: 50,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        boxShadow: const [
          BoxShadow(
            blurRadius: 10,
            offset: Offset(0, 4),
            color: Color(0x0E000000),
          ),
        ],
      ),
      child: TextField(
        controller: _searchController,
        onChanged: (value) => setState(() => _keyword = value),
        decoration: const InputDecoration(
          prefixIcon: Icon(Icons.search, color: Colors.grey),
          hintText: 'Tìm theo tên, môn học, số điện thoại...',
          hintStyle: TextStyle(color: Colors.grey),
          border: InputBorder.none,
          contentPadding: EdgeInsets.symmetric(vertical: 14),
        ),
      ),
    );
  }

  Widget _buildTabs() {
    return Row(
      children: [
        Expanded(
          child: _TabButton(
            label: 'Giáo viên',
            selected: _tabIndex == 0,
            onTap: () => setState(() => _tabIndex = 0),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _TabButton(
            label: 'Nhà trường',
            selected: _tabIndex == 1,
            onTap: () => setState(() => _tabIndex = 1),
          ),
        ),
      ],
    );
  }

  Widget _buildTeacherList() {
    final items = _filteredTeachers;

    if (items.isEmpty) {
      return const _EmptyState(
        icon: Icons.contact_phone_outlined,
        text: 'Không tìm thấy liên hệ giáo viên',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
      itemCount: items.length,
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (_, index) {
        final item = items[index];
        final displayName = _normalizeAliasText(item.displayName);
        final normalizedNote = item.note.trim();

        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(22),
            boxShadow: const [
              BoxShadow(
                blurRadius: 12,
                offset: Offset(0, 4),
                color: Color(0x12000000),
              ),
            ],
          ),
          child: Column(
            children: [
              Row(
                children: [
                  Container(
                    width: 52,
                    height: 52,
                    decoration: BoxDecoration(
                      color: item.highlight
                          ? const Color(0xFFFFF1E6)
                          : const Color(0xFFF4F4F4),
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: Icon(
                      item.highlight ? Icons.star_rounded : Icons.person_outline,
                      color: item.highlight ? orange : Colors.black54,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                displayName,
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w800,
                                  color: textDark,
                                ),
                              ),
                            ),
                            if (item.highlight)
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 10,
                                  vertical: 6,
                                ),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFFFF1E6),
                                  borderRadius: BorderRadius.circular(999),
                                ),
                                child: const Text(
                                  'Chủ nhiệm',
                                  style: TextStyle(
                                    color: orange,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${item.subjectName} · ${item.role}',
                          style: const TextStyle(
                            fontSize: 13,
                            color: textGrey,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              _InfoRow(
                icon: Icons.badge_outlined,
                label: 'Tên thật',
                value: item.fullName,
              ),
              const SizedBox(height: 8),
              _InfoRow(
                icon: Icons.room_outlined,
                label: 'Phòng học',
                value: item.roomName,
              ),
              const SizedBox(height: 8),
              _InfoRow(
                icon: Icons.phone_outlined,
                label: 'Số điện thoại',
                value: item.phone,
              ),
              const SizedBox(height: 8),
              _InfoRow(
                icon: Icons.email_outlined,
                label: 'Email',
                value: item.email,
              ),
              if (normalizedNote.isNotEmpty &&
                  _normalizeAliasText(normalizedNote) != displayName) ...[
                const SizedBox(height: 10),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF9F9F9),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Text(
                    _normalizeAliasText(normalizedNote),
                    style: const TextStyle(
                      fontSize: 13,
                      color: textGrey,
                      height: 1.4,
                    ),
                  ),
                ),
              ],
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                    child: _ActionButton(
                      icon: Icons.call_rounded,
                      label: 'Gọi điện',
                      filled: true,
                      onTap: () => _callPhone(item.phone),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _ActionButton(
                      icon: Icons.mail_outline_rounded,
                      label: 'Email',
                      filled: false,
                      onTap: () => _sendEmail(item.email),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildSchoolList() {
    final items = _filteredSchools;

    if (items.isEmpty) {
      return const _EmptyState(
        icon: Icons.apartment_outlined,
        text: 'Không tìm thấy liên hệ nhà trường',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
      itemCount: items.length,
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (_, index) {
        final item = items[index];
        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(22),
            boxShadow: const [
              BoxShadow(
                blurRadius: 12,
                offset: Offset(0, 4),
                color: Color(0x12000000),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 50,
                    height: 50,
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF1E6),
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: const Icon(
                      Icons.domain_outlined,
                      color: orange,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      item.departmentName,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                        color: textDark,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              _InfoRow(
                icon: Icons.person_outline_rounded,
                label: 'Người liên hệ',
                value: item.contactName,
              ),
              const SizedBox(height: 8),
              _InfoRow(
                icon: Icons.phone_outlined,
                label: 'Số điện thoại',
                value: item.phone,
              ),
              const SizedBox(height: 8),
              _InfoRow(
                icon: Icons.email_outlined,
                label: 'Email',
                value: item.email,
              ),
              if (item.address.trim().isNotEmpty) ...[
                const SizedBox(height: 8),
                _InfoRow(
                  icon: Icons.location_on_outlined,
                  label: 'Địa chỉ',
                  value: item.address,
                ),
              ],
              if (item.description.trim().isNotEmpty) ...[
                const SizedBox(height: 10),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF9F9F9),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Text(
                    item.description,
                    style: const TextStyle(
                      fontSize: 13,
                      color: textGrey,
                      height: 1.4,
                    ),
                  ),
                ),
              ],
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                    child: _ActionButton(
                      icon: Icons.call_rounded,
                      label: 'Gọi điện',
                      filled: true,
                      onTap: () => _callPhone(item.phone),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _ActionButton(
                      icon: Icons.mail_outline_rounded,
                      label: 'Email',
                      filled: false,
                      onTap: () => _sendEmail(item.email),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}

class _TabButton extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _TabButton({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);

    return InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: onTap,
      child: Container(
        height: 44,
        decoration: BoxDecoration(
          color: selected ? const Color(0xFFFFF1E6) : Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: selected ? orange : const Color(0xFFE7E7E7),
          ),
        ),
        child: Center(
          child: Text(
            label,
            style: TextStyle(
              color: selected ? orange : Colors.black87,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ),
    );
  }
}

class _ActionButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool filled;
  final VoidCallback onTap;

  const _ActionButton({
    required this.icon,
    required this.label,
    required this.filled,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);

    return SizedBox(
      height: 46,
      child: ElevatedButton.icon(
        style: ElevatedButton.styleFrom(
          elevation: 0,
          backgroundColor: filled ? orange : const Color(0xFFFFF1E6),
          foregroundColor: filled ? Colors.white : orange,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
        onPressed: onTap,
        icon: Icon(icon, size: 18),
        label: Text(
          label,
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    const textGrey = Color(0xFF7B7B7B);
    const textDark = Color(0xFF222222);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: textGrey),
        const SizedBox(width: 8),
        SizedBox(
          width: 100,
          child: Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              color: textGrey,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
        Expanded(
          child: Text(
            value.isEmpty ? '--' : value,
            style: const TextStyle(
              fontSize: 13,
              color: textDark,
              fontWeight: FontWeight.w600,
              height: 1.35,
            ),
          ),
        ),
      ],
    );
  }
}

class _EmptyState extends StatelessWidget {
  final IconData icon;
  final String text;

  const _EmptyState({
    required this.icon,
    required this.text,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 42, color: Colors.grey),
            const SizedBox(height: 10),
            Text(
              text,
              style: const TextStyle(
                color: Colors.grey,
                fontSize: 14,
                fontWeight: FontWeight.w600,
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

String _normalizeAliasText(String value) {
  var text = value.trim();
  if (text.isEmpty) {
    return text;
  }

  text = text.replaceFirst(RegExp(r'^\s*alias\s*:\s*', caseSensitive: false), '');

  final pipeIndex = text.indexOf('|');
  if (pipeIndex >= 0) {
    text = text.substring(0, pipeIndex).trim();
  }

  return text;
}

