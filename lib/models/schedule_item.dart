class ScheduleItem {
  final int teacherId;
  final String teacherAlias;
  final String teacherName;
  final String teacherPhone;
  final String teacherEmail;
  final String teacherRole;
  final int dayOfWeek;
  final int periodNo;
  final String startTime;
  final String endTime;
  final String subjectName;
  final String roomName;
  final String note;

  ScheduleItem({
    required this.teacherId,
    required this.teacherAlias,
    required this.teacherName,
    required this.teacherPhone,
    required this.teacherEmail,
    required this.teacherRole,
    required this.dayOfWeek,
    required this.periodNo,
    required this.startTime,
    required this.endTime,
    required this.subjectName,
    required this.roomName,
    required this.note,
  });

  factory ScheduleItem.fromJson(Map<String, dynamic> json) {
    return ScheduleItem(
      teacherId: _toInt(json['teacherId']),
      teacherAlias: json['teacherAlias']?.toString() ?? '',
      teacherName: json['teacherName']?.toString() ?? '',
      teacherPhone: json['teacherPhone']?.toString() ?? '',
      teacherEmail: json['teacherEmail']?.toString() ?? '',
      teacherRole: json['teacherRole']?.toString() ?? '',
      dayOfWeek: _toInt(json['dayOfWeek']),
      periodNo: _toInt(json['periodNo']),
      startTime: json['startTime']?.toString() ?? '',
      endTime: json['endTime']?.toString() ?? '',
      subjectName: json['subjectName']?.toString() ?? '',
      roomName: json['roomName']?.toString() ?? '',
      note: json['note']?.toString() ?? '',
    );
  }

  static int _toInt(dynamic value) {
    if (value is int) {
      return value;
    }
    return int.tryParse(value?.toString() ?? '') ?? 0;
  }
}
