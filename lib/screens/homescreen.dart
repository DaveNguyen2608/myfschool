import 'package:flutter/material.dart';

import '../models/news_item.dart';
import '../services/news_service.dart';
import '../services/notification_service.dart';
import 'chat_list_page.dart';
import 'club_page.dart';
import 'contact_screen.dart';
import 'news_detail_page.dart';
import 'notification_list_page.dart';
import 'profile_page.dart';
import 'request_screen.dart';
import 'schedule.dart';
import 'score_page.dart';
import 'teacher_score_page.dart';

class HomeScreen extends StatefulWidget {
  final String studentName;
  final String studentCode;
  final String className;
  final String campusName;
  final String parentUsername;
  final bool isTeacher;

  const HomeScreen({
    super.key,
    required this.studentName,
    required this.studentCode,
    required this.className,
    required this.campusName,
    required this.parentUsername,
    this.isTeacher = false,
  });

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  static const Color orange = Color(0xFFFF7A00);

  int _currentIndex = 0;
  final NotificationService _notificationService = NotificationService();
  final NewsService _newsService = NewsService();
  final PageController _featuredPageController = PageController(
    viewportFraction: 0.92,
  );

  int _unreadNotificationCount = 0;
  bool _loadingNews = true;
  String? _newsError;
  List<NewsItem> _featuredNews = const [];
  List<NewsItem> _latestNews = const [];
  int _featuredPageIndex = 0;

  @override
  void initState() {
    super.initState();
    _loadUnreadNotificationCount();
    _loadNews();
  }

  @override
  void dispose() {
    _featuredPageController.dispose();
    super.dispose();
  }

  Future<void> _loadUnreadNotificationCount() async {
    try {
      final count = await _notificationService.getUnreadCount(
        username: widget.parentUsername,
      );
      if (!mounted) return;
      setState(() {
        _unreadNotificationCount = count;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _unreadNotificationCount = 0;
      });
    }
  }

  Future<void> _loadNews() async {
    setState(() {
      _loadingNews = true;
      _newsError = null;
    });

    try {
      final results = await Future.wait<List<NewsItem>>([
        _newsService.getFeatured(username: widget.parentUsername, take: 5),
        _newsService.getNews(username: widget.parentUsername, take: 10),
      ]);

      final featured = results[0];
      final latestRaw = results[1];
      final featuredIds = featured.map((e) => e.id).toSet();
      final latest = latestRaw
          .where((x) => !featuredIds.contains(x.id))
          .toList();

      if (!mounted) return;
      setState(() {
        _featuredNews = featured;
        _latestNews = latest.isNotEmpty ? latest : latestRaw;
        _featuredPageIndex = 0;
      });

      if (_featuredPageController.hasClients &&
          _featuredPageController.page != 0) {
        _featuredPageController.jumpToPage(0);
      }
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _newsError = 'Không tải được tin tức. Vui lòng thử lại.';
      });
    } finally {
      if (mounted) {
        setState(() {
          _loadingNews = false;
        });
      }
    }
  }

  Future<void> _openNotifications() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NotificationListPage(
          username: widget.parentUsername,
          isTeacher: widget.isTeacher,
        ),
      ),
    );

    if (!mounted) return;
    await _loadUnreadNotificationCount();
  }

  Future<void> _openNewsDetail(NewsItem item) async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NewsDetailPage(
          username: widget.parentUsername,
          newsId: item.id,
          initialItem: item,
        ),
      ),
    );
  }

  void _showComingSoon() {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Chức năng đang phát triển')));
  }

  String _formatDate(DateTime? dateTime) {
    if (dateTime == null) return '--/--/----';
    final local = dateTime.toLocal();
    final dd = local.day.toString().padLeft(2, '0');
    final mm = local.month.toString().padLeft(2, '0');
    final yyyy = local.year.toString();
    return '$dd/$mm/$yyyy';
  }

  String _mapArticleTypeLabel(String type) {
    switch (type.trim().toUpperCase()) {
      case 'NOTICE':
        return 'Thông báo';
      case 'ACTIVITY':
        return 'Hoạt động';
      case 'EVENT':
        return 'Sự kiện';
      default:
        return 'Tin tức';
    }
  }

  Widget _buildNewsCover({
    required String? imageUrl,
    required double height,
    BorderRadius? borderRadius,
  }) {
    final normalized = (imageUrl ?? '').trim();
    final fallback = Container(
      height: height,
      decoration: BoxDecoration(
        color: const Color(0xFFEFEFEF),
        borderRadius: borderRadius,
      ),
      alignment: Alignment.center,
      child: const Icon(
        Icons.image_not_supported_outlined,
        size: 34,
        color: Colors.black38,
      ),
    );

    if (normalized.isEmpty) {
      return fallback;
    }

    final image = SizedBox(
      height: height,
      width: double.infinity,
      child: Image.network(
        normalized,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => fallback,
      ),
    );

    if (borderRadius == null) return image;
    return ClipRRect(borderRadius: borderRadius, child: image);
  }

  Widget _buildFeaturedSection() {
    if (_loadingNews && _featuredNews.isEmpty) {
      return Container(
        width: double.infinity,
        height: 190,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
        ),
        alignment: Alignment.center,
        child: const CircularProgressIndicator(),
      );
    }

    if (_featuredNews.isEmpty) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
        ),
        child: Column(
          children: [
            const Icon(Icons.article_outlined, color: Colors.black38, size: 36),
            const SizedBox(height: 8),
            const Text(
              'Chưa có tin nổi bật',
              style: TextStyle(
                color: Colors.black54,
                fontWeight: FontWeight.w600,
              ),
            ),
            if (_newsError != null) ...[
              const SizedBox(height: 8),
              Text(
                _newsError!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.red, fontSize: 12),
              ),
              const SizedBox(height: 8),
              OutlinedButton(
                onPressed: _loadNews,
                child: const Text('Tải lại'),
              ),
            ],
          ],
        ),
      );
    }

    return Column(
      children: [
        SizedBox(
          height: 190,
          child: PageView.builder(
            controller: _featuredPageController,
            itemCount: _featuredNews.length,
            onPageChanged: (index) {
              setState(() {
                _featuredPageIndex = index;
              });
            },
            itemBuilder: (_, index) {
              final item = _featuredNews[index];
              return Padding(
                padding: const EdgeInsets.only(right: 8),
                child: InkWell(
                  borderRadius: BorderRadius.circular(18),
                  onTap: () => _openNewsDetail(item),
                  child: Ink(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(18),
                      boxShadow: const [
                        BoxShadow(
                          blurRadius: 10,
                          offset: Offset(0, 3),
                          color: Color(0x12000000),
                        ),
                      ],
                    ),
                    child: Stack(
                      children: [
                        _buildNewsCover(
                          imageUrl: item.coverImageUrl,
                          height: 190,
                          borderRadius: BorderRadius.circular(18),
                        ),
                        Positioned(
                          left: 0,
                          right: 0,
                          bottom: 0,
                          child: Container(
                            padding: const EdgeInsets.fromLTRB(12, 12, 12, 12),
                            decoration: BoxDecoration(
                              borderRadius: const BorderRadius.vertical(
                                bottom: Radius.circular(18),
                              ),
                              gradient: LinearGradient(
                                begin: Alignment.bottomCenter,
                                end: Alignment.topCenter,
                                colors: [
                                  Colors.black.withValues(alpha: 0.72),
                                  Colors.transparent,
                                ],
                              ),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 8,
                                    vertical: 3,
                                  ),
                                  decoration: BoxDecoration(
                                    color: const Color(0xFFFFE7D1),
                                    borderRadius: BorderRadius.circular(999),
                                  ),
                                  child: Text(
                                    _mapArticleTypeLabel(item.articleType),
                                    style: const TextStyle(
                                      color: orange,
                                      fontSize: 11,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  item.title,
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 14,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ),
        const SizedBox(height: 10),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: List.generate(_featuredNews.length, (index) {
            final isActive = _featuredPageIndex == index;
            return AnimatedContainer(
              duration: const Duration(milliseconds: 220),
              width: isActive ? 18 : 8,
              height: 8,
              margin: const EdgeInsets.symmetric(horizontal: 3),
              decoration: BoxDecoration(
                color: isActive ? orange : const Color(0xFFD8D8D8),
                borderRadius: BorderRadius.circular(999),
              ),
            );
          }),
        ),
      ],
    );
  }

  Widget _buildLatestSection() {
    if (_loadingNews && _latestNews.isEmpty) {
      return Container(
        width: double.infinity,
        height: 110,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
        ),
        alignment: Alignment.center,
        child: const CircularProgressIndicator(),
      );
    }

    if (_latestNews.isEmpty) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
        ),
        child: const Text(
          'Chưa có tin mới hoặc hoạt động gần đây.',
          style: TextStyle(color: Colors.black54, fontWeight: FontWeight.w600),
        ),
      );
    }

    return ListView.separated(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: _latestNews.length,
      separatorBuilder: (_, _) => const SizedBox(height: 10),
      itemBuilder: (_, index) {
        final item = _latestNews[index];
        return InkWell(
          onTap: () => _openNewsDetail(item),
          borderRadius: BorderRadius.circular(14),
          child: Ink(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(14),
              boxShadow: const [
                BoxShadow(
                  blurRadius: 8,
                  offset: Offset(0, 2),
                  color: Color(0x12000000),
                ),
              ],
            ),
            child: Row(
              children: [
                SizedBox(
                  width: 88,
                  height: 66,
                  child: _buildNewsCover(
                    imageUrl: item.coverImageUrl,
                    height: 66,
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.title,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          fontSize: 13.5,
                        ),
                      ),
                      const SizedBox(height: 5),
                      Text(
                        (item.summary ?? '').trim().isEmpty
                            ? 'Bấm để xem chi tiết bài viết'
                            : item.summary!.trim(),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Colors.black54,
                          fontSize: 12.5,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          Text(
                            _mapArticleTypeLabel(item.articleType),
                            style: const TextStyle(
                              color: orange,
                              fontSize: 11.5,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          const SizedBox(width: 8),
                          const Text(
                            '•',
                            style: TextStyle(color: Colors.black45),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            _formatDate(item.publishedAt),
                            style: const TextStyle(
                              color: Colors.black45,
                              fontSize: 11.5,
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

  @override
  Widget build(BuildContext context) {
    const bg = Color(0xFFF3F3F3);
    final isTeacher = widget.isTeacher;

    final menuItems = [
      _MenuData(
        icon: Icons.send_rounded,
        label: isTeacher ? 'Xem đơn' : 'Gửi đơn',
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => RequestScreen(
                mode: isTeacher ? UserViewMode.teacher : UserViewMode.parent,
                username: widget.parentUsername,
              ),
            ),
          );
        },
      ),
      _MenuData(icon: Icons.event, label: 'Sự kiện', onTap: _showComingSoon),
      _MenuData(
        icon: Icons.science_outlined,
        label: 'Câu lạc bộ',
        onTap: () {
          if (isTeacher) {
            _showComingSoon();
            return;
          }

          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => ClubPage(username: widget.parentUsername),
            ),
          );
        },
      ),
      _MenuData(
        icon: Icons.calendar_month_outlined,
        label: 'Lịch học',
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => SchedulePage(
                username: widget.parentUsername,
                displayName: widget.studentName,
                className: widget.className,
                isTeacher: isTeacher,
              ),
            ),
          );
        },
      ),
      _MenuData(
        icon: Icons.call,
        label: 'Liên lạc',
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => ContactScreen(
                username: widget.parentUsername,
                studentName: widget.studentName,
                className: widget.className,
                isTeacher: isTeacher,
              ),
            ),
          );
        },
      ),
      _MenuData(
        icon: Icons.edit_document,
        label: 'KT&KL',
        onTap: _showComingSoon,
      ),
      _MenuData(
        icon: Icons.assignment_outlined,
        label: 'Bảng điểm',
        onTap: () {
          if (isTeacher) {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) =>
                    TeacherScorePage(username: widget.parentUsername),
              ),
            );
            return;
          }

          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => ScorePage(username: widget.parentUsername),
            ),
          );
        },
      ),
      _MenuData(
        icon: Icons.directions_bus_filled_outlined,
        label: 'Đưa đón',
        onTap: _showComingSoon,
      ),
    ];

    final displayNameLabel = isTeacher ? 'Tên giáo viên: ' : 'Tên học sinh: ';
    final displayNameValue = widget.studentName.isNotEmpty
        ? widget.studentName
        : 'Chưa có dữ liệu';

    final codeLabel = isTeacher ? 'Tài khoản: ' : 'MSHS: ';
    final codeValue = isTeacher
        ? widget.parentUsername
        : (widget.studentCode.isNotEmpty ? widget.studentCode : '--');

    return Scaffold(
      backgroundColor: bg,
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(18, 10, 18, 10),
              child: Column(
                children: [
                  Row(
                    children: [
                      const _Avatar(),
                      const Spacer(),
                      InkWell(
                        borderRadius: BorderRadius.circular(999),
                        onTap: _openNotifications,
                        child: Container(
                          width: 42,
                          height: 42,
                          decoration: const BoxDecoration(
                            color: Colors.white,
                            shape: BoxShape.circle,
                          ),
                          child: Stack(
                            clipBehavior: Clip.none,
                            children: [
                              const Center(
                                child: Icon(
                                  Icons.notifications_none,
                                  color: orange,
                                  size: 24,
                                ),
                              ),
                              if (_unreadNotificationCount > 0)
                                Positioned(
                                  right: -2,
                                  top: -2,
                                  child: Container(
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 5,
                                      vertical: 2,
                                    ),
                                    decoration: BoxDecoration(
                                      color: Colors.red,
                                      borderRadius: BorderRadius.circular(10),
                                    ),
                                    constraints: const BoxConstraints(
                                      minWidth: 16,
                                    ),
                                    child: Text(
                                      _unreadNotificationCount > 99
                                          ? '99+'
                                          : _unreadNotificationCount.toString(),
                                      textAlign: TextAlign.center,
                                      style: const TextStyle(
                                        color: Colors.white,
                                        fontSize: 9.5,
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
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
                    children: [
                      Text(
                        displayNameLabel,
                        style: const TextStyle(
                          fontSize: 13,
                          color: Colors.black87,
                        ),
                      ),
                      Expanded(
                        child: Text(
                          displayNameValue,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Text(
                        'Lớp: ',
                        style: TextStyle(fontSize: 13, color: Colors.black87),
                      ),
                      Text(
                        widget.className.isNotEmpty ? widget.className : '--',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(width: 10),
                      const Text('•', style: TextStyle(color: Colors.black54)),
                      const SizedBox(width: 10),
                      Text(
                        codeLabel,
                        style: const TextStyle(
                          fontSize: 13,
                          color: Colors.black87,
                        ),
                      ),
                      Expanded(
                        child: Text(
                          codeValue,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Text(
                        'Cơ sở: ',
                        style: TextStyle(fontSize: 13, color: Colors.black87),
                      ),
                      Expanded(
                        child: Text(
                          widget.campusName.isNotEmpty
                              ? widget.campusName
                              : '--',
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14),
              child: Column(
                children: [
                  GridView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: menuItems.length,
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 4,
                          crossAxisSpacing: 12,
                          mainAxisSpacing: 12,
                          mainAxisExtent: 110,
                        ),
                    itemBuilder: (context, index) {
                      final item = menuItems[index];
                      return _MenuTile(
                        icon: item.icon,
                        label: item.label,
                        onTap: item.onTap,
                      );
                    },
                  ),
                  const SizedBox(height: 10),
                  Center(
                    child: Container(
                      width: 140,
                      height: 8,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(999),
                      ),
                      child: Align(
                        alignment: Alignment.centerLeft,
                        child: Container(
                          width: 70,
                          height: 8,
                          decoration: BoxDecoration(
                            color: orange,
                            borderRadius: BorderRadius.circular(999),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            Expanded(
              child: RefreshIndicator(
                onRefresh: () async {
                  await _loadUnreadNotificationCount();
                  await _loadNews();
                },
                child: SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Text(
                            'Tin nổi bật',
                            style: TextStyle(
                              fontSize: 17,
                              fontWeight: FontWeight.w800,
                              color: Colors.black87,
                            ),
                          ),
                          const Spacer(),
                          if (_newsError != null)
                            TextButton(
                              onPressed: _loadNews,
                              child: const Text('Tải lại'),
                            ),
                        ],
                      ),
                      _buildFeaturedSection(),
                      const SizedBox(height: 16),
                      const Text(
                        'Tin mới / Hoạt động gần đây',
                        style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w800,
                          color: Colors.black87,
                        ),
                      ),
                      const SizedBox(height: 10),
                      _buildLatestSection(),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: Container(
        padding: const EdgeInsets.fromLTRB(10, 10, 10, 10),
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
          boxShadow: [
            BoxShadow(
              blurRadius: 16,
              offset: Offset(0, -2),
              color: Color(0x14000000),
            ),
          ],
        ),
        child: BottomNavigationBar(
          currentIndex: _currentIndex,
          onTap: _onBottomNavTap,
          type: BottomNavigationBarType.fixed,
          backgroundColor: Colors.transparent,
          elevation: 0,
          selectedItemColor: orange,
          unselectedItemColor: const Color(0xFFB5B5B5),
          showSelectedLabels: false,
          showUnselectedLabels: false,
          items: const [
            BottomNavigationBarItem(
              icon: Icon(Icons.home_rounded),
              label: 'Trang chủ',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.bar_chart_rounded),
              label: 'Thống kê',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.chat_bubble_outline_rounded),
              label: 'Chat',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.person_outline_rounded),
              label: 'Cá nhân',
            ),
          ],
        ),
      ),
    );
  }

  void _onBottomNavTap(int i) {
    if (i == 3) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ProfilePage(
            studentName: widget.studentName,
            parentUsername: widget.parentUsername,
            className: widget.className,
            studentCode: widget.studentCode,
            campusName: widget.campusName,
            isTeacher: widget.isTeacher,
          ),
        ),
      );
      return;
    }

    if (i == 2) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ChatListPage(
            username: widget.parentUsername,
            isTeacher: widget.isTeacher,
          ),
        ),
      );
      return;
    }

    if (i == 1) {
      _showComingSoon();
      return;
    }

    setState(() => _currentIndex = i);
  }
}

class _MenuData {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  _MenuData({required this.icon, required this.label, required this.onTap});
}

class _Avatar extends StatelessWidget {
  const _Avatar();

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);

    return Container(
      width: 46,
      height: 46,
      decoration: BoxDecoration(
        color: Colors.white,
        shape: BoxShape.circle,
        border: Border.all(color: orange, width: 2),
      ),
      child: const Icon(Icons.person, color: orange, size: 26),
    );
  }
}

class _MenuTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _MenuTile({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFFF7A00);

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 58,
            height: 58,
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              boxShadow: const [
                BoxShadow(
                  blurRadius: 10,
                  offset: Offset(0, 3),
                  color: Color(0x12000000),
                ),
              ],
            ),
            child: Icon(icon, color: orange, size: 26),
          ),
          const SizedBox(height: 6),
          Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}
