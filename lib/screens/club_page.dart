import 'package:flutter/material.dart';
import '../models/club_item.dart';
import '../services/club_service.dart';
import 'club_detail_page.dart';

class ClubPage extends StatefulWidget {
  final String username;

  const ClubPage({super.key, required this.username});

  @override
  State<ClubPage> createState() => _ClubPageState();
}

class _ClubPageState extends State<ClubPage>
    with SingleTickerProviderStateMixin {
  final ClubService _clubService = ClubService();

  late TabController _tabController;
  late Future<List<ClubItem>> _allClubsFuture;
  late Future<List<ClubItem>> _myClubsFuture;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _loadData();
  }

  void _loadData() {
    _allClubsFuture = _clubService.getAllClubs(widget.username);
    _myClubsFuture = _clubService.getMyClubs(widget.username);
  }

  Future<void> _refresh() async {
    setState(() {
      _loadData();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);

    return Scaffold(
      backgroundColor: const Color(0xFFF7F7F7),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        centerTitle: true,
        title: const Text(
          'Câu lạc bộ',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w700,
          ),
        ),
        iconTheme: const IconThemeData(color: Colors.black),
      ),
      body: Column(
        children: [
          Container(
            color: Colors.white,
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
            child: Container(
              height: 44,
              decoration: BoxDecoration(
                color: const Color(0xFFF9F4EF),
                borderRadius: BorderRadius.circular(6),
              ),
              child: TabBar(
                controller: _tabController,
                dividerColor: Colors.transparent,
                indicator: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(4),
                ),
                labelColor: orange,
                unselectedLabelColor: const Color(0xFFBEBEBE),
                labelStyle: const TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 13,
                ),
                tabs: const [
                  Tab(text: 'DANH SÁCH CLB'),
                  Tab(text: 'CLB CỦA TÔI'),
                ],
              ),
            ),
          ),
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                RefreshIndicator(
                  onRefresh: _refresh,
                  child: FutureBuilder<List<ClubItem>>(
                    future: _allClubsFuture,
                    builder: (context, snapshot) {
                      return _buildList(
                        snapshot,
                        emptyText: 'Chưa có câu lạc bộ nào',
                      );
                    },
                  ),
                ),
                RefreshIndicator(
                  onRefresh: _refresh,
                  child: FutureBuilder<List<ClubItem>>(
                    future: _myClubsFuture,
                    builder: (context, snapshot) {
                      return _buildList(
                        snapshot,
                        emptyText: 'Bạn chưa đăng ký câu lạc bộ nào',
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildList(
      AsyncSnapshot<List<ClubItem>> snapshot, {
        required String emptyText,
      }) {
    if (snapshot.connectionState == ConnectionState.waiting) {
      return const Center(child: CircularProgressIndicator());
    }

    if (snapshot.hasError) {
      return ListView(
        children: [
          const SizedBox(height: 120),
          Center(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Text('Lỗi tải dữ liệu: ${snapshot.error}'),
            ),
          ),
        ],
      );
    }

    final clubs = snapshot.data ?? [];
    if (clubs.isEmpty) {
      return ListView(
        children: [
          const SizedBox(height: 120),
          Center(child: Text(emptyText)),
        ],
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: clubs.length,
      separatorBuilder: (_, __) => const SizedBox(height: 14),
      itemBuilder: (context, index) {
        final club = clubs[index];

        return InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: () async {
            await Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => ClubDetailPage(
                  clubId: club.id,
                  username: widget.username,
                ),
              ),
            );
            await _refresh();
          },
          child: Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 58,
                  height: 58,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: const Color(0xFFFF9C3B),
                      width: 2,
                    ),
                  ),
                  child: const Icon(
                    Icons.person,
                    color: Color(0xFFFFB54A),
                    size: 34,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        club.clubName,
                        style: const TextStyle(
                          color: Color(0xFFFF7A00),
                          fontWeight: FontWeight.w700,
                          fontSize: 15,
                        ),
                      ),
                      const SizedBox(height: 6),
                      _clubText(
                        'Số slot: ',
                        '${club.registeredCount}/${club.slotLimit}',
                        highlight: true,
                      ),
                      _clubText('Từ: ', club.startDate, highlight: true),
                      _clubText('Đến: ', club.endDate, highlight: true),
                      Row(
                        children: [
                          const Text(
                            'Trạng thái: ',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          Text(
                            club.isRegistered ? 'Đã đăng ký' : 'Chưa đăng ký',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: club.isRegistered
                                  ? Colors.green
                                  : Colors.red,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _clubText(String label, String value, {bool highlight = false}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 2),
      child: RichText(
        text: TextSpan(
          style: const TextStyle(color: Colors.black87, fontSize: 13),
          children: [
            TextSpan(
              text: label,
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
            TextSpan(
              text: value,
              style: TextStyle(
                fontWeight: FontWeight.w700,
                color: highlight ? const Color(0xFFFF7A00) : Colors.black87,
              ),
            ),
          ],
        ),
      ),
    );
  }
}