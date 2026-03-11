import 'package:flutter/material.dart';
import 'schedule.dart';
import 'club_page.dart';

class HomeScreen extends StatefulWidget {
  final String fullName;
  final String username;
  const HomeScreen({
    super.key,
    required this.fullName,
    required this.username,
  });
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}


class _HomeScreenState extends State<HomeScreen> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final bg = const Color(0xFFF3F3F3);
    final orange = const Color(0xFFFF7A00);

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
                      _Avatar(),
                      const Spacer(),
                      InkWell(
                        borderRadius: BorderRadius.circular(999),
                        onTap: () {},
                        child: Container(
                          width: 42,
                          height: 42,
                          decoration: const BoxDecoration(
                            color: Colors.white,
                            shape: BoxShape.circle,
                          ),
                          child: Icon(Icons.notifications_none,
                              color: orange, size: 24),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),


                  Row(
                    children: [
                      const Text("Tên học sinh: ",
                          style: TextStyle(fontSize: 13, color: Colors.black87)),
                      Text(
                        widget.fullName.isNotEmpty ? widget.fullName : "Nguyễn Hữu Tiến",
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),

                  // Info lines
                  Row(
                    children: const [
                      Text("Lớp: ",
                          style: TextStyle(fontSize: 13, color: Colors.black87)),
                      Text(
                        "PRM392",
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      SizedBox(width: 10),
                      Text("·", style: TextStyle(color: Colors.black54)),
                      SizedBox(width: 10),
                      Text("MSHS: ",
                          style: TextStyle(fontSize: 13, color: Colors.black87)),
                      Text(
                        "HE182008",
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: const [
                      Text("Cơ sở: ",
                          style: TextStyle(fontSize: 13, color: Colors.black87)),
                      Text(
                        "Hola",
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
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
                  GridView.count(
                    crossAxisCount: 4,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: 12,
                    crossAxisSpacing: 12,
                    childAspectRatio: 0.80,
                    children: [
                      _MenuTile(
                        icon: Icons.send_rounded,
                        label: "Gửi đơn",
                        onTap: () {
                          // TODO: Navigator.push(...) tới trang Gửi đơn
                        },
                      ),
                      _MenuTile(
                        icon: Icons.event,
                        label: "Sự kiện",
                        onTap: () {
                          // TODO
                        },
                      ),
                      _MenuTile(
                        icon: Icons.science_outlined,
                        label: "Câu lạc bộ",
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => ClubPage(username: widget.username),
                            ),
                          );
                        },
                      ),
                      _MenuTile(
                        icon: Icons.calendar_month_outlined,
                        label: "Lịch học",
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                                builder: (_) => SchedulePage(username: widget.username)
                            ),
                          );
                        },
                      ),
                      _MenuTile(
                        icon: Icons.call,
                        label: "Liên lạc",
                        onTap: () {
                          // TODO
                        },
                      ),
                      _MenuTile(
                        icon: Icons.edit_document,
                        label: "KT&KL",
                        onTap: () {
                          // TODO
                        },
                      ),
                      _MenuTile(
                        icon: Icons.assignment_outlined,
                        label: "Bảng điểm",
                        onTap: () {
                          // TODO
                        },
                      ),
                      _MenuTile(
                        icon: Icons.directions_bus_filled_outlined,
                        label: "Đưa đón",
                        onTap: () {
                          // TODO
                        },
                      ),
                    ],
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
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
                child: Column(
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(18),
                      child: AspectRatio(
                        aspectRatio: 16 / 9,
                        child: Image.network(
                          // bạn thay link/banner thật của bạn ở đây
                          "assets/images/thong-bao-nghi-tet-nguyen-dan-2026-3.png",
                          fit: BoxFit.cover,
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(18),
                      ),
                      child: const Text(
                        "[P.TC&QLDT] TB V/v nghỉ lễ Tết Nguyên Đán, Cung chúc Tân Xuân...",
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
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
          onTap: (i) => setState(() => _currentIndex = i),
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
              label: "Trang chủ",
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.bar_chart_rounded),
              label: "Thống kê",
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.chat_bubble_outline_rounded),
              label: "Chat",
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.person_outline_rounded),
              label: "Cá nhân",
            ),
          ],
        ),
      ),
    );
  }
}

class _Avatar extends StatelessWidget {
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
            style: const TextStyle(
              fontSize: 12.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}