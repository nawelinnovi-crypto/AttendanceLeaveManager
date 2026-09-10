export interface Employee {
  id: string | number;
  fullName: string;
  email: string;
  department: string;
  jobTitle: string;
  avatarUrl: string;
  role: 'Employee' | 'Manager';
  shiftSchedule: string;
  contractType: 'SIVP' | 'CDD' | 'CDI';
  hireDate: string;
}

export interface AttendanceRecord {
  id: string | number;
  employeeId: string | number;
  employeeName: string;
  department: string;
  date: string;
  clockInTime?: string;
  clockOutTime?: string;
  status: 'OnTime' | 'Late' | 'EarlyDeparture' | 'Absent';
  workingHours: number;
  notes: string;
}

export interface LeaveRequest {
  id: string | number;
  employeeId: string | number;
  employeeName: string;
  department: string;
  leaveType: 'Annual' | 'Sick' | 'Unpaid' | 'Telework' | 'Maternity';
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Canceled';
  submittedDate: string;
  managerComment?: string;
}

export interface LeaveBalance {
  employeeId: string | number;
  annualTotal: number;
  annualUsed: number;
  annualRemaining: number;
  sickTotal: number;
  sickUsed: number;
  sickRemaining: number;
  teleworkAllowed: number;
  teleworkUsed: number;
  unpaidUsed: number;
}

export interface ClockInRequest {
  employeeId: string | number;
  notes?: string;
}

export interface ClockOutRequest {
  employeeId: string | number;
  notes?: string;
}

export interface ManualAttendanceRequest {
  employeeId: string | number;
  date: string;
  clockIn: string;
  clockOut: string;
  notes: string;
}

export interface CreateLeaveRequest {
  employeeId: string | number;
  leaveType: string;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface LeaveApprovalRequest {
  approved: boolean;
  managerComment?: string;
}

export interface DashboardSummary {
  totalEmployees: number;
  presentToday: number;
  lateToday: number;
  onLeaveToday: number;
  pendingLeavesCount: number;
  recentPointages: AttendanceRecord[];
  recentLeaves: LeaveRequest[];
}

export interface CompanyRegisterReq {
  companyName: string;
  adminName: string;
  email: string;
  password: string;
  plan: string;
}

export interface LoginReq {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  companyId: string;
  companyName: string;
  adminName: string;
  email: string;
  role: string;
  plan: string;
}

export interface Company {
  id: string;
  companyName: string;
  companyCode: string;
  adminName: string;
  email: string;
  plan: string;
  status: string;
  subscriptionDate: string;
}
