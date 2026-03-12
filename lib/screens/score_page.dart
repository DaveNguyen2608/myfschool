import 'package:flutter/material.dart';
import '../models/academic_year_item.dart';
import '../models/score_item.dart';
import '../models/score_summary.dart';
import '../services/score_service.dart';

class ScorePage extends StatefulWidget {
  final String username;

  const ScorePage({super.key, required this.username});

  @override
  State<ScorePage> createState() => _ScorePageState();
}

class _ScorePageState extends State<ScorePage> {
  final ScoreService _scoreService = ScoreService();
  final TextEditingController _searchController = TextEditingController();

  List<AcademicYearItem> _years = [];
  AcademicYearItem? _selectedYear;

  int _mainTabIndex = 0; // 0 = Bang diem, 1 = Hanh kiem
  int _semesterNo = 1; // 1,2,3(ca nam)

  bool _loadingYears = true;
  bool _loadingData = false;

  ScoreSummary? _summary;
  List<ScoreItem> _scores = [];
  List<ScoreItem> _filteredScores = [];

  @override
  void initState() {
    super.initState();
    _loadAcademicYears();
    _searchController.addListener(_filterScores);
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadAcademicYears() async {
    setState(() {
      _loadingYears = true;
    });

    try {
      final years = await _scoreService.getAcademicYears();
      if (!mounted) return;

      setState(() {
        _years = years;
        if (_years.isNotEmpty) {
          _selectedYear = _years.first;
        }
      });

      if (_selectedYear != null) {
        await _loadScoreData();
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi tải năm học: $e')),
      );
    } finally {
      if (mounted) {
        setState(() {
          _loadingYears = false;
        });
      }
    }
  }

  Future<void> _loadScoreData() async {
    if (_selectedYear == null) return;

    setState(() {
      _loadingData = true;
    });

    try {
      final summary = await _scoreService.getScoreSummary(
        username: widget.username,
        academicYearId: _selectedYear!.id,
        semesterNo: _semesterNo,
      );

      final scores = await _scoreService.getScores(
        username: widget.username,
        academicYearId: _selectedYear!.id,
        semesterNo: _semesterNo,
      );

      if (!mounted) return;

      setState(() {
        _summary = summary;
        _scores = scores;
        _filteredScores = scores;
      });

      _filterScores();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi tải bảng điểm: $e')),
      );
    } finally {
      if (mounted) {
        setState(() {
          _loadingData = false;
        });
      }
    }
  }

  void _filterScores() {
    final keyword = _searchController.text.trim().toLowerCase();

    setState(() {
      _filteredScores = _scores.where((item) {
        return item.subjectName.toLowerCase().contains(keyword);
      }).toList();
    });
  }

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);
    const lightBg = Color(0xFFF6F6F6);

    return Scaffold(
      backgroundColor: lightBg,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        centerTitle: true,
        title: const Text(
          'Bảng điểm',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w700,
          ),
        ),
        iconTheme: const IconThemeData(color: Colors.black),
      ),
      body: _loadingYears
          ? const Center(child: CircularProgressIndicator())
          : _selectedYear == null
          ? const Center(child: Text('Không có năm học'))
          : RefreshIndicator(
        onRefresh: _loadScoreData,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildYearDropdown(),
              const SizedBox(height: 14),
              _buildMainTabs(orange),
              const SizedBox(height: 14),
              _buildSemesterTabs(orange),
              const SizedBox(height: 14),
              if (_mainTabIndex == 0) ...[
                _buildSummaryCard(),
                const SizedBox(height: 14),
                _buildSearchBox(),
                const SizedBox(height: 14),
                _loadingData
                    ? const Center(child: CircularProgressIndicator())
                    : _buildScoreList(),
              ] else ...[
                _buildConductView(),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildYearDropdown() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: const Color(0xFFE2E2E2)),
        borderRadius: BorderRadius.circular(8),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<AcademicYearItem>(
          value: _selectedYear,
          isExpanded: true,
          items: _years.map((year) {
            return DropdownMenuItem(
              value: year,
              child: Text(
                year.name,
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
            );
          }).toList(),
          onChanged: (value) async {
            setState(() {
              _selectedYear = value;
            });
            await _loadScoreData();
          },
        ),
      ),
    );
  }

  Widget _buildMainTabs(Color orange) {
    return Row(
      children: [
        Expanded(
          child: GestureDetector(
            onTap: () {
              setState(() => _mainTabIndex = 0);
            },
            child: Container(
              height: 42,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: Colors.white,
                border: Border(
                  bottom: BorderSide(
                    color: _mainTabIndex == 0 ? orange : Colors.transparent,
                    width: 2.5,
                  ),
                ),
              ),
              child: Text(
                'BẢNG ĐIỂM',
                style: TextStyle(
                  color: _mainTabIndex == 0 ? orange : Colors.grey,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
        ),
        Expanded(
          child: GestureDetector(
            onTap: () {
              setState(() => _mainTabIndex = 1);
            },
            child: Container(
              height: 42,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: Colors.white,
                border: Border(
                  bottom: BorderSide(
                    color: _mainTabIndex == 1 ? orange : Colors.transparent,
                    width: 2.5,
                  ),
                ),
              ),
              child: Text(
                'HẠNH KIỂM',
                style: TextStyle(
                  color: _mainTabIndex == 1 ? orange : Colors.grey,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildSemesterTabs(Color orange) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: const Color(0xFFEAEAEA)),
      ),
      child: Row(
        children: [
          _semesterTab('Kỳ 1', 1, orange),
          _semesterTab('Kỳ 2', 2, orange),
          _semesterTab('Cả năm', 3, orange),
        ],
      ),
    );
  }

  Widget _semesterTab(String text, int value, Color orange) {
    final selected = _semesterNo == value;

    return Expanded(
      child: GestureDetector(
        onTap: () async {
          setState(() {
            _semesterNo = value;
          });
          await _loadScoreData();
        },
        child: Container(
          height: 42,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            border: Border(
              right: value != 3
                  ? const BorderSide(color: Color(0xFFEAEAEA))
                  : BorderSide.none,
            ),
          ),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            decoration: BoxDecoration(
              border: selected
                  ? Border.all(color: orange, width: 1.6)
                  : null,
            ),
            child: Text(
              text,
              style: TextStyle(
                color: selected ? orange : Colors.grey,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSummaryCard() {
    final s = _summary;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
      ),
      child: s == null
          ? const Text('Chưa có dữ liệu')
          : Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _summaryLine('Trung bình:', s.averageScore.toStringAsFixed(1),
              valueColor: const Color(0xFFFF8A00)),
          const SizedBox(height: 6),
          _summaryLine('Học lực:', s.academicPerformance,
              valueColor: const Color(0xFFFF8A00)),
          const SizedBox(height: 6),
          _summaryLine('Hạnh kiểm:', s.conduct,
              valueColor: const Color(0xFFFF8A00)),
          const SizedBox(height: 6),
          _summaryLine('Chú ý:', s.note.isEmpty ? '...' : s.note),
        ],
      ),
    );
  }

  Widget _summaryLine(String label, String value, {Color? valueColor}) {
    return Row(
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            color: Colors.black87,
          ),
        ),
        const SizedBox(width: 6),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w700,
              color: valueColor ?? Colors.black87,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildSearchBox() {
    return TextField(
      controller: _searchController,
      decoration: InputDecoration(
        hintText: 'Tìm kiếm môn học ...',
        filled: true,
        fillColor: Colors.white,
        prefixIcon: const Icon(Icons.search),
        contentPadding: const EdgeInsets.symmetric(vertical: 0, horizontal: 12),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFFE2E2E2)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFFFF8A00)),
        ),
      ),
    );
  }

  Widget _buildScoreList() {
    if (_filteredScores.isEmpty) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(10),
        ),
        child: const Center(
          child: Text('Không có dữ liệu môn học'),
        ),
      );
    }

    return Column(
      children: _filteredScores.map((item) {
        return Container(
          margin: const EdgeInsets.only(bottom: 12),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(30),
            boxShadow: const [
              BoxShadow(
                color: Color(0x11000000),
                blurRadius: 6,
                offset: Offset(0, 2),
              ),
            ],
          ),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  item.subjectName.toUpperCase(),
                  style: const TextStyle(
                    fontWeight: FontWeight.w700,
                    color: Color(0xFF3A556A),
                  ),
                ),
              ),
              Text(
                item.result.isNotEmpty
                    ? item.result
                    : item.averageScore?.toStringAsFixed(1) ?? '--',
                style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: item.result == 'Đạt'
                      ? const Color(0xFFFF8A00)
                      : const Color(0xFFFF8A00),
                ),
              ),
            ],
          ),
        );
      }).toList(),
    );
  }

  Widget _buildConductView() {
    final s = _summary;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
      ),
      child: _loadingData
          ? const Center(child: CircularProgressIndicator())
          : Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _summaryLine('Năm học:', _selectedYear?.name ?? ''),
          const SizedBox(height: 8),
          _summaryLine(
            'Kỳ:',
            _semesterNo == 1
                ? 'Kỳ 1'
                : _semesterNo == 2
                ? 'Kỳ 2'
                : 'Cả năm',
          ),
          const SizedBox(height: 8),
          _summaryLine('Hạnh kiểm:', s?.conduct ?? 'Tốt',
              valueColor: const Color(0xFFFF8A00)),
          const SizedBox(height: 8),
          _summaryLine('Nhận xét:', s?.note.isEmpty == false ? s!.note : 'Chăm ngoan, có tiến bộ'),
        ],
      ),
    );
  }
}