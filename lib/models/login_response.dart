class LoginResponse {
  final int id;
  final String username;
  final String fullName;
  final String? email;
  final String status;
  final String message;
  final String roleCode;
  final List<String> roleCodes;
  final String accessToken;
  final String tokenType;
  final DateTime? expiresAtUtc;

  final String? studentName;
  final String? studentCode;
  final String? className;
  final String? campusName;

  LoginResponse({
    required this.id,
    required this.username,
    required this.fullName,
    this.email,
    required this.status,
    required this.message,
    required this.roleCode,
    required this.roleCodes,
    required this.accessToken,
    required this.tokenType,
    required this.expiresAtUtc,
    this.studentName,
    this.studentCode,
    this.className,
    this.campusName,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    final rawRoleCodes = json['roleCodes'];
    final parsedRoleCodes = rawRoleCodes is List
        ? rawRoleCodes.map((e) => e.toString()).toList(growable: false)
        : <String>[];

    DateTime? parsedExpiresAt;
    final expiresRaw = json['expiresAtUtc']?.toString();
    if (expiresRaw != null && expiresRaw.trim().isNotEmpty) {
      parsedExpiresAt = DateTime.tryParse(expiresRaw);
    }

    return LoginResponse(
      id: json['id'] is int ? json['id'] : int.parse(json['id'].toString()),
      username: json['username']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      email: json['email']?.toString(),
      status: json['status']?.toString() ?? '',
      message: json['message']?.toString() ?? '',
      roleCode: json['roleCode']?.toString() ?? '',
      roleCodes: parsedRoleCodes,
      accessToken: json['accessToken']?.toString() ?? '',
      tokenType: json['tokenType']?.toString() ?? 'Bearer',
      expiresAtUtc: parsedExpiresAt,
      studentName: json['studentName']?.toString(),
      studentCode: json['studentCode']?.toString(),
      className: json['className']?.toString(),
      campusName: json['campusName']?.toString(),
    );
  }
}
